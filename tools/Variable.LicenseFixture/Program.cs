using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

if (args.Length != 3 || !Guid.TryParse(args[0], out var organization) || !int.TryParse(args[1], out var maximum) || maximum < 0)
    throw new ArgumentException("Usage: Variable.LicenseFixture <organization-uuid> <max-active-learners> <output-directory>");
var directory = Path.GetFullPath(args[2]); Directory.CreateDirectory(directory);
var privatePath = Path.Combine(directory, "test-private.pem"); var publicPath = Path.Combine(directory, "test-public.pem");
using var issuer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
if (File.Exists(privatePath)) issuer.ImportFromPem(await File.ReadAllTextAsync(privatePath));
else
{
    await File.WriteAllTextAsync(privatePath, issuer.ExportPkcs8PrivateKeyPem());
    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(privatePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
}
await File.WriteAllTextAsync(publicPath, issuer.ExportSubjectPublicKeyInfoPem());
var now = DateTimeOffset.UtcNow;
// Millisecond epoch gives synthetic regenerations a practically monotonic issuer revision.
var revision = now.ToUnixTimeMilliseconds();
var payload = JsonSerializer.Serialize(new { schema_version = 1, product = "variable-lms", license_id = $"development-only-{revision}",
    organization_id = organization.ToString(), revision, issued_at = now.AddMinutes(-1).ToString("O"), not_before = now.AddMinutes(-1).ToString("O"),
    expires_at = now.AddYears(1).ToString("O"), entitlements = new { max_active_learners = maximum },
    required_entitlements = new[] { "max_active_learners" }, extensions = new { synthetic = true } });
var key = new ECDsaSecurityKey(issuer) { KeyId = "development_test_issuer", CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } };
var document = new JsonWebTokenHandler().CreateToken(payload, new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256),
    new Dictionary<string, object> { ["typ"] = "variable-lms-license" });
await File.WriteAllTextAsync(Path.Combine(directory, "license.jwt"), document);
Console.WriteLine($"Synthetic development license written to {directory}");
