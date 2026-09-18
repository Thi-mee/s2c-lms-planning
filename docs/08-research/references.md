# Technical sources used for the baseline

> **Status:** Verified during consolidation on 2026-09-17\
> **Authority:** Supporting evidence, not product requirements

## Purpose

Primary sources used to correct technical assumptions in the ADRs. Product decisions come from the accepted decision record, not external examples. Library/tool details must be checked again when selecting actual implementation versions.

| Source | Claim checked / consequence |
|---|---|
| [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | .NET 10 is LTS; pin a supported servicing version. |
| [PostgreSQL row security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html) | Superusers/BYPASSRLS and normally table owners bypass policies. Non-superuser alone is insufficient. RLS is separate from scoped authorization. |
| [EF Core applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) | Production migration privileges and live traffic require deliberate execution; locking does not establish application compatibility. |
| [ASP.NET Core cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) | Cookies need validation to reflect changed/disabled users; do not assume immediate built-in revocation. |
| [Cookie SessionStore](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.cookieauthenticationoptions.sessionstore?view=aspnetcore-10.0) | Ticket-store integration supports sending only a session identifier to the client. PostgreSQL implementation is our engineering choice. |
| [ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) | Persistent protected key management is separate from session records; container replacement must not discard needed keys. |
| [EF Core NativeAOT](https://learn.microsoft.com/en-us/ef/core/performance/nativeaot-and-precompiled-queries) | Current limitations do not justify making NativeAOT a production baseline. |
| [Helm charts](https://helm.sh/docs/topics/charts/) | Chart version and appVersion are distinct; appVersion is informational, not a compatibility check. |
| [Compose services](https://docs.docker.com/reference/compose-file/services/) | Image references support explicit versions/digests; package compatibility remains our release responsibility. |
| [RFC 7515 — JWS](https://www.rfc-editor.org/rfc/rfc7515.html), [RFC 7518 — algorithms](https://www.rfc-editor.org/rfc/rfc7518.html) | Standard signature envelope/ES256 encoding for the engineering license wire profile, with application-specific schema and trust rules. |

## Open questions and related documents

Sources do not decide product lifecycle policy, support targets or commercial terms. See the [question register](../00-product/open-questions.md), [ADR log](../06-architecture/decisions.md) and [historical evaluation](architecture-evaluation.md).
