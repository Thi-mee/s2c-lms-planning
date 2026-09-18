# Identity foundation HTTP contract

> **Status:** Implemented foundation\
> **Authority:** Canonical — current first-party HTTP contract; domain role policy remains authoritative\
> **Updated:** 2026-09-18

## Purpose

The initial same-origin React/ASP.NET Core slice. All identifiers belong to the configured organization; browser headers/bodies cannot select another organization. JSON request fields not declared below are rejected. These endpoints do not imply that general account management or the full LMS is complete.

## Endpoints

| Method/path | Request | Success and authority |
|---|---|---|
| `GET /health/live` | None | 200 process alive; no account needed. |
| `GET /health/ready` | None | 200 database/runtime/schema/organization compatible; otherwise 503. |
| `GET /api/auth/csrf` | Cookie credentials | 200 `{ "token": "…" }`, plus anti-forgery cookie. Anonymous allowed. |
| `POST /api/auth/login` | `{ "email": "…", "password": "…" }` | 204 and session cookie for an active, usable account. |
| `GET /api/auth/session` | Session cookie | 200 `{ "account": { "id", "name", "email", "roles" }, "organization": { "id", "name" } }`. |
| `POST /api/auth/logout` | Empty JSON object | 204, deletes stored ticket and expires cookie. Authentication required. |
| `POST /api/administration/users/{id}/administrator-role` | `{ "granted": true/false, "currentPassword": "…", "reason": "…" }` | 204; dedicated Administrator-only, freshly reauthenticated operation. |
| `POST /api/administration/users/{id}/deactivate` | `{ "reason": "…", "currentPassword": "…" }` | 204; Administrator, or Organization Manager targeting ordinary accounts only. Current password required for an Administrator target. |

Every POST requires the anti-forgery cookie plus `X-CSRF-TOKEN`. Obtain a fresh token for the current identity; login/logout changes that identity. Cookie/session details and exact engineering defaults live in the [operational guide](../06-architecture/local-development.md#concrete-security-defaults).

Administrator assignments and revocations use the [canonical policy](../01-domain/roles-and-permissions.md), never the ordinary delegation helper. A target receiving Administrator must be active, initialized and not temporarily locked out. Removal/deactivation cannot leave the organization without another usable Administrator. Organization Manager cannot promote itself or deactivate an Administrator/Manager. Resource grants confer none of these privileges.

Both security commands recheck actor state/version and authority under the organization transaction lock. Role/status mutation, version invalidation, ticket deletion and required audit commit together. Repeating an already-applied desired role/status is a no-op while the caller remains authorized. Foreign-organization targets return 404. Self-revocation/deactivation signs the actor out. Reasons are required, nonempty and at most 1,000 characters.

## Errors and privacy

Domain/persistence failures use `{ "code": "…", "requestId": "…" }`; callers must also handle bare HTTP authentication, binding and rate-limit errors. Unknown/disabled accounts and bad passwords share `401 invalid_credentials`. No account-existence distinction appears in the login UI.

| Status | Typical meaning |
|---|---|
| 400 | Invalid CSRF token, unknown/malformed request fields, missing reason or non-HTTPS production request. |
| 401 | Missing/expired/revoked session, invalid credentials or changed current identity. |
| 403 | Forbidden operation or failed current-password reauthentication. |
| 404 | Route absent or target absent within the authorized organization. |
| 409 | Final usable Administrator guard, or unusable Administrator assignment target. |
| 429 | Login/security-operation rate limit reached. |
| 503 | Database unavailable, incompatible schema/runtime identity, or required persistence/audit failed. |

No bootstrap HTTP route exists. The one-time operator command is in [local development](../06-architecture/local-development.md). No credential/token/password hash is returned in account DTOs or logged. Session responses use `Cache-Control: no-store`. The first-party API and frontend ship together; this is not a public third-party API version commitment.

## Validation and remaining scope

The next slice implements account lookup and the narrow Course Author membership command, documented with [course authoring](course-authoring.md#role-management-addition). Other ordinary role grants, activation and invitations remain unimplemented until their own capacity/authorization paths are in place.

[PostgreSQL integration tests](../../tests/Variable.IntegrationTests/IdentityTests.cs) cover negative authorization, organization isolation, CSRF, privilege races, rollback, migration locking and revocation/restart/expiry. [Browser tests](../../src/web/tests/authentication.spec.ts) exercise the compiled frontend. Ordinary roles, invitations, activation/reactivation, resource grants and password recovery receive contracts with their own slices and capacity checks. No gate in the [question register](../00-product/open-questions.md) is silently resolved by this contract.
