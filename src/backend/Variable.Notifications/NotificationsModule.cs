using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Variable.Database;
using Variable.Identity;

namespace Variable.Notifications;

public sealed record EmailMessage(string Recipient, string Subject, string TextBody);

public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public interface INotificationDispatcher
{
    Task<bool> RunOnceAsync(CancellationToken ct = default);
}

public static class NotificationsModule
{
    public static ModuleMigration Migration => ModuleMigration.Embedded(typeof(NotificationsModule).Assembly, 5, "0005-notifications", "notifications", """
        GRANT USAGE ON SCHEMA notifications TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE ON notifications.email_intents TO {runtime_role};
        """);

    public static IServiceCollection AddVariableNotifications(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = NotificationOptions.Read(configuration, environment);
        services.AddSingleton(options);
        services.AddScoped<IInvitationEmailWriter, InvitationEmailWriter>();
        services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();
        if (!environment.IsEnvironment("Testing")) services.AddHostedService<NotificationWorker>();
        return services;
    }
}

internal sealed class InvitationEmailWriter(IDataProtectionProvider protection, TimeProvider clock) : IInvitationEmailWriter
{
    private readonly IDataProtector protector = protection.CreateProtector("Variable.LMS.Notification.Invitation.v1");

    public async Task EnqueueAsync(IdentityWriteContext context, Guid userId, string email, string token,
        DateTimeOffset expiresAt, int invitationVersion, CancellationToken ct)
    {
        var payload = protector.Protect(JsonSerializer.Serialize(new InvitationPayload(token, expiresAt)));
        await using var command = new NpgsqlCommand("""
            INSERT INTO notifications.email_intents(id,organization_id,recipient_user_id,event_type,recipient_email,
                protected_payload,dedupe_key,status,attempts,next_attempt_at,created_at)
            VALUES ($1,$2,$3,'invitation',$4,$5,$6,'pending',0,$7,$7)
            ON CONFLICT (organization_id,dedupe_key) DO NOTHING
            """, (NpgsqlConnection)context.Connection, (NpgsqlTransaction)context.Transaction);
        var now = clock.GetUtcNow();
        command.Parameters.AddWithValue(Guid.NewGuid()); command.Parameters.AddWithValue(context.Actor.Organization.Id);
        command.Parameters.AddWithValue(userId); command.Parameters.AddWithValue(email); command.Parameters.AddWithValue(payload);
        command.Parameters.AddWithValue($"invitation:{userId:N}:{invitationVersion}"); command.Parameters.AddWithValue(now);
        await command.ExecuteNonQueryAsync(ct);
    }

    internal sealed record InvitationPayload(string Token, DateTimeOffset ExpiresAt);
}

internal sealed class NotificationDispatcher(NotificationOptions options, IDataProtectionProvider protection,
    IEmailTransport transport, TimeProvider clock, ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    private readonly IDataProtector protector = protection.CreateProtector("Variable.LMS.Notification.Invitation.v1");

    public async Task<bool> RunOnceAsync(CancellationToken ct = default)
    {
        var claimed = await ClaimAsync(ct); if (claimed is null) return false;
        try
        {
            InvitationEmailWriter.InvitationPayload payload;
            try { payload = JsonSerializer.Deserialize<InvitationEmailWriter.InvitationPayload>(protector.Unprotect(claimed.ProtectedPayload))!; }
            catch (Exception error) when (error is CryptographicException or JsonException)
            { await FinishAsync(claimed, false, "payload_unprotect_failed", ct); return true; }
            if (!await InvitationStillCurrentAsync(claimed, payload, ct))
            { await FinishAsync(claimed, false, "invitation_no_longer_current", ct, terminal: true); return true; }
            var link = new Uri(options.PublicOrigin, "/accept-invitation?token=" + Uri.EscapeDataString(payload.Token));
            await transport.SendAsync(new(claimed.Email, "Your Variable LMS invitation",
                $"You have been invited to Variable LMS. Accept the invitation before {payload.ExpiresAt:u}: {link}"), ct);
            await FinishAsync(claimed, true, null, ct); return true;
        }
        catch (Exception error) when (error is SmtpException or IOException or TimeoutException)
        {
            logger.LogWarning("Invitation email delivery failed ({ErrorType}); durable work will be retried", error.GetType().Name);
            await FinishAsync(claimed, false, "smtp_delivery_failed", ct); return true;
        }
    }

    private async Task<ClaimedIntent?> ClaimAsync(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(options.ConnectionString); await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var lease = Guid.NewGuid(); var now = clock.GetUtcNow();
        await using var command = new NpgsqlCommand("""
            WITH candidate AS (
              SELECT id FROM notifications.email_intents
              WHERE ((status='pending' AND next_attempt_at <= $1) OR (status='processing' AND lease_until <= $1))
              ORDER BY next_attempt_at,created_at FOR UPDATE SKIP LOCKED LIMIT 1)
            UPDATE notifications.email_intents i SET status='processing',lease_until=$2,lease_token=$3,attempts=attempts+1
            FROM candidate WHERE i.id=candidate.id
            RETURNING i.id,i.organization_id,i.recipient_user_id,i.recipient_email,i.protected_payload,i.attempts
            """, connection, transaction);
        command.Parameters.AddWithValue(now); command.Parameters.AddWithValue(now.AddMinutes(2)); command.Parameters.AddWithValue(lease);
        await using var reader = await command.ExecuteReaderAsync(ct);
        ClaimedIntent? intent = await reader.ReadAsync(ct) ? new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2),
            reader.GetString(3), reader.GetString(4), reader.GetInt32(5), lease) : null;
        await reader.DisposeAsync(); await transaction.CommitAsync(ct); return intent;
    }

    private async Task<bool> InvitationStillCurrentAsync(ClaimedIntent intent, InvitationEmailWriter.InvitationPayload payload, CancellationToken ct)
    {
        if (payload.ExpiresAt <= clock.GetUtcNow()) return false;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.Token)));
        await using var connection = new NpgsqlConnection(options.ConnectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT FROM identity.invitations WHERE organization_id=$1 AND user_id=$2
                AND token_hash=$3 AND used_at IS NULL AND expires_at>$4)
            """, connection);
        command.Parameters.AddWithValue(intent.OrganizationId); command.Parameters.AddWithValue(intent.UserId);
        command.Parameters.AddWithValue(hash); command.Parameters.AddWithValue(clock.GetUtcNow());
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    private async Task FinishAsync(ClaimedIntent intent, bool sent, string? error, CancellationToken ct, bool terminal = false)
    {
        var now = clock.GetUtcNow(); var failed = terminal || intent.Attempts >= options.MaximumAttempts;
        var status = sent ? "sent" : failed ? "failed" : "pending";
        var delaySeconds = Math.Min(3600, 15 * (1 << Math.Min(intent.Attempts - 1, 8)));
        await using var connection = new NpgsqlConnection(options.ConnectionString); await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE notifications.email_intents SET status=$1,next_attempt_at=$2,lease_until=NULL,lease_token=NULL,
                last_error_code=$3,sent_at=CASE WHEN $1='sent' THEN $4 ELSE sent_at END
            WHERE id=$5 AND lease_token=$6
            """, connection);
        command.Parameters.AddWithValue(status); command.Parameters.AddWithValue(now.AddSeconds(sent ? 0 : delaySeconds));
        command.Parameters.AddWithValue((object?)error ?? DBNull.Value); command.Parameters.AddWithValue(now);
        command.Parameters.AddWithValue(intent.Id); command.Parameters.AddWithValue(intent.LeaseToken);
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record ClaimedIntent(Guid Id, Guid OrganizationId, Guid UserId, string Email,
        string ProtectedPayload, int Attempts, Guid LeaseToken);
}

internal sealed class NotificationWorker(INotificationDispatcher dispatcher, ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await dispatcher.RunOnceAsync(stoppingToken)) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception error)
            {
                logger.LogError("Notification worker cycle failed ({ErrorType})", error.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}

internal sealed class SmtpEmailTransport(NotificationOptions options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        using var mail = new MailMessage(options.FromAddress, message.Recipient, message.Subject, message.TextBody);
        using var smtp = new SmtpClient(options.SmtpHost, options.SmtpPort) { EnableSsl = options.UseTls };
        if (options.Username is not null)
            smtp.Credentials = new NetworkCredential(options.Username, File.ReadAllText(options.PasswordFile!).TrimEnd('\r', '\n'));
        await smtp.SendMailAsync(mail, ct);
    }
}

internal sealed record NotificationOptions(string ConnectionString, Uri PublicOrigin, string SmtpHost, int SmtpPort,
    bool UseTls, string FromAddress, string? Username, string? PasswordFile, int MaximumAttempts)
{
    internal static NotificationOptions Read(IConfiguration configuration, IHostEnvironment environment)
    {
        var connection = configuration.GetConnectionString("Runtime") ?? throw new InvalidOperationException("ConnectionStrings:Runtime is required.");
        if (!Uri.TryCreate(configuration["Installation:PublicOrigin"], UriKind.Absolute, out var origin))
            throw new InvalidOperationException("Installation:PublicOrigin is required for invitation links.");
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) && !environment.IsDevelopment() && !environment.IsEnvironment("Testing")) throw new InvalidOperationException("Smtp:Host is required.");
        host ??= environment.IsDevelopment() ? "localhost" : "disabled.test";
        var port = configuration.GetValue("Smtp:Port", environment.IsDevelopment() ? 1025 : 587);
        if (port is < 1 or > 65535) throw new InvalidOperationException("Smtp:Port must be between 1 and 65535.");
        var security = configuration["Smtp:Security"] ?? (environment.IsDevelopment() || environment.IsEnvironment("Testing") ? "None" : "StartTls");
        if (security is not ("None" or "StartTls")) throw new InvalidOperationException("Smtp:Security must be None or StartTls.");
        if (security == "None" && !environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("Unencrypted SMTP is permitted only in Development/Testing.");
        var from = configuration["Smtp:FromAddress"] ?? "variable@example.test";
        if (!MailAddress.TryCreate(from, out _)) throw new InvalidOperationException("Smtp:FromAddress must be a valid address.");
        var username = configuration["Smtp:Username"]; var passwordFile = configuration["Smtp:PasswordFile"];
        if ((username is null) != (passwordFile is null) || (passwordFile is not null && (!Path.IsPathFullyQualified(passwordFile) || !File.Exists(passwordFile))))
            throw new InvalidOperationException("Smtp:Username and an existing absolute Smtp:PasswordFile must be supplied together.");
        var maximumAttempts = configuration.GetValue("Notifications:MaximumAttempts", 8);
        if (maximumAttempts is < 1 or > 100) throw new InvalidOperationException("Notifications:MaximumAttempts must be between 1 and 100.");
        return new(connection, origin, host, port, security == "StartTls", from, username, passwordFile, maximumAttempts);
    }
}
