# Security, identity and organization boundary

> **Status:** Confirmed engineering baseline\
> **Authority:** Canonical — ADR-9 and ADR-11 implementation guidance\
> **Updated:** 2026-09-17

## Purpose

Define enforceable security behavior for the initial organization-owned deployment. Product role authority is defined only in [roles and permissions](../01-domain/roles-and-permissions.md).

## Organization boundary

One customer organization per initial installation. Each account belongs to one organization; email is normalized and unique within that organization. No global identity directory, organization switching, cross-tenant admin role or SaaS provisioning workflow is implied.

Every organization-owned table includes `organization_id`. Use tenant-consistent foreign keys/constraints where possible; validate cohort-course, lesson-course, submission-enrollment and forum-scope consistency, not only organization equality. Obtain organization context from trusted installation/session identity, never from a freely supplied browser header. Compare requested identifiers to that context. A second-organization fixture must be inaccessible even if IDs are guessed.

Database identities:

| Identity | Privileges | Availability |
|---|---|---|
| Web runtime / hosted worker | Non-owner, non-superuser; only required data operations; no DDL, no BYPASSRLS | Normal runtime |
| Migration runner | Schema ownership/DDL only as required | Explicit deployment step, credentials absent from web runtime |
| Backup/recovery operator | Enough scope for complete backup/restore | Operator-controlled workflow, not an LMS role |

RLS is not required by the single-organization MVP. If later chosen, policies must default-deny missing context, cover reads/writes, use transaction-local context for pooled connections, and cover jobs and integration reads. Never confuse non-superuser with non-owner: owners bypass RLS by default. Composite relationships still matter because foreign-key integrity checks are not a substitute for policies. The [PostgreSQL documentation](https://www.postgresql.org/docs/current/ddl-rowsecurity.html) is the source for these behaviors.

## Bootstrap and Administrator continuity

Use an operator-controlled, one-time bootstrap command/flow scoped to the configured organization. It creates the organization and first active Administrator with initialized credentials; no reusable default password and no public endpoint granting the first caller ownership. Provision the signed license as an installation input, but staff-only Administrator setup consumes zero learner seats.

Persist a bootstrap completion marker and audit. Re-running ordinary bootstrap cannot add another privileged account. Later Administrator changes require the dedicated privileged operation defined in the role matrix and fresh authentication. Protect the final usable Administrator under concurrent role removal, deactivation and soft deletion. Pending, deleted or credential-uninitialized Administrators are not usable substitutes. Define temporary lockout handling so automated abuse protection does not erase administrative recovery.

No in-app bypass of the final-Administrator guard is provided. If operator recovery is later needed, document an authenticated-by-host-control, audited maintenance procedure before exposing it; never weaken authorization because recovery might exist someday.

## Credentials, invitation and sessions

Use ASP.NET Core Identity credential hashing and token primitives; set a documented password policy and auth rate limits before exposure. Tokens are purpose/organization/user-bound, expire, are single-use where required, and are not stored or logged in recoverable plaintext. Invitation resend invalidates earlier acceptance tokens. Duplicate invite retries do not create duplicate users. Reset tokens are distinct from invitation tokens.

The SPA uses Secure, HttpOnly cookies, SameSite protection and anti-forgery tokens on state-changing requests; safe HTTP methods do not change state. TLS termination and trusted reverse-proxy headers are deployment requirements. Do not put session tokens in browser local storage.

Use a PostgreSQL-backed opaque ticket/session store initially, with expiry and per-user security version. Validate both session and current active user/security version on protected requests. Disable/revoke on deactivation, soft deletion, password reset, and role/security changes. Recheck current authority within sensitive write transactions to handle revocation racing an already-running request. Resource-grant checks read current grants; revocation must affect subsequent protected actions even if an account role remains.

Data-protection keys protect cookies/tokens; they are not session state. Persist keys outside the container layer and protect access/encryption according to [Microsoft guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0). A Redis cache is optional, never assumed to make revocation automatic. Deny protected requests when authoritative authentication state cannot be validated; do not silently fail open.

## Content and data safety

Sanitize lesson rich text and forum Markdown on the server; reject executable URL schemes and unauthorized remote fetches. No arbitrary user HTML certificate templates. Validate logo type/size and use safe decoding; never execute uploaded content. Serve learner PDFs through authorized access, not guessable public storage paths. An opaque certificate ID is not a digital signature or an authorized public verification page.

Log correlation IDs and actionable failures without credentials, license secrets, answer keys, tokens or unnecessary personal data. Score/key fields never leak in learner quiz-delivery DTOs. Audit privileged changes in the same transaction; external log delivery is separate. Retention/erasure of named snapshots and audit records remains [Q57](../00-product/open-questions.md#q57); this baseline does not claim legal certification.

## Open questions

[Q57](../00-product/open-questions.md#q57) (retention), [Q73](../00-product/open-questions.md#q73) (quiz review/test permissions), [Q75](../00-product/open-questions.md#q75) (certificate replacement). Session timeouts/rate thresholds are implementation defaults to document and test, not new product blockers.

## Related documents

[Role policy](../01-domain/roles-and-permissions.md) · [User](../03-data-model/user.md) · [Audit](../03-data-model/audit-log.md) · [Operations](release-and-operations.md)
