# API design

> **Status:** Confirmed engineering baseline; endpoint contracts will be added per implementation slice\
> **Authority:** Supporting conventions; feature behavior and role policy remain canonical\
> **Updated:** 2026-09-18

## Purpose and current contracts

[License contract](license-contract.md) is the external LMS/issuer boundary. [Identity foundation](identity-foundation.md) and [course authoring](course-authoring.md) document the implemented HTTP slices. There is no generated OpenAPI yet. Define each further HTTP contract alongside its vertical slice using the [template](api-template.md); when OpenAPI is added, generate/validate it from the implementation rather than maintaining a divergent parallel API.

## First-party API conventions

Use same-origin HTTP/JSON over TLS between the React/Vite application and ASP.NET Core host. Cookie sessions, CSRF protection, current account/grant validation and explicit organization/resource checks are mandatory. There is no JWT/GraphQL stack decision outstanding. Public routes are limited to necessary authentication/token/bootstrap surfaces under their own controls, not a public learning catalog or certificate verifier.

Use explicit commands for activation, role delegation, enrollment, quiz submission/reset and completion-related actions rather than generic unrestricted CRUD. The authenticated context supplies organization identity; a body/header organization ID is never authority. Reject attempts to alter immutable parent/run fields. Resource checks and write preconditions occur in the use-case transaction, not solely middleware.

DTOs expose the minimum needed data; never serialize ORM graphs, answer keys, password/token material or another cohort's data. Use consistent typed error codes, field errors and request IDs; redact internals. Choose exact status mappings/pagination conventions during the first slice. Lists are bounded and deterministically ordered. Search remains scoped to authorized courses/cohorts/forums; PostgreSQL filtering/indexes suffice initially.

## Retry and compatibility

Mutating requests that create externally meaningful outcomes carry scoped idempotency/request keys with payload comparison: retries return the same result, while reusing a key for a different command payload is rejected. Database constraints remain necessary for semantic uniqueness, even when two requests use different keys. Apply to invitations, enrollment, submissions, resets and issuance-triggering workflows; natural idempotent updates need no generic framework.

The first-party frontend ships with the backend release. Future public API/version support is a separate commitment. Persisted work and license documents already have explicit version handling because they survive process upgrades.

## Validation and open questions

Test negative organization/resource scope, stale/deactivated sessions, forbidden payload fields, CSRF, capacity/allowance races and retries for each slice. Read the feature's links to the [question register](../00-product/open-questions.md); endpoint names and error shapes are engineering decisions, not new product blockers.

## Related documents

[Features](../02-features/README.md) · [Roles](../01-domain/roles-and-permissions.md) · [Security](../06-architecture/security-and-identity.md) · [Module boundaries](../06-architecture/module-boundaries.md)
