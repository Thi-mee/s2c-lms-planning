# Entity: Audit Log

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Identity & Organization audit facility; each business module owns its action semantics\
> **Updated:** 2026-09-17

## Purpose

Authoritative, append-only evidence for sensitive operations, persisted with their state changes.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| actor_user_id | reference, optional | Actor; absent for system/bootstrap, with explicit actor kind |
| actor_kind | enum, required | user, system, operator |
| action | text, required | Stable action identifier |
| target_type / target_id | text / reference | Affected resource identity |
| metadata | bounded structured data | Redacted reason, before/after values, request correlation |
| created_at | timestamp, required | Event time |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Append-only during ordinary operation. Required audits include user/role/security changes, privileged administrator operations, staff/membership, course publication, license loads/capacity changes, attempt resets, certificate issuance/void/reissue and moderation. Failed authorization attempts are security logs; avoid exposing secret payloads there too.

Audit persistence joins the business transaction. Failure rolls back the sensitive mutation; exporting the committed record to an external collector may fail independently. This explicitly replaces the earlier best-effort authoritative-audit rule. Ordinary API roles cannot edit/delete audit entries. Avoid unnecessary PII and never store passwords, invite/reset tokens, session tickets, signing keys or full quiz answer keys in audit metadata. Retention/erasure is a dedicated approved maintenance operation, not a generic delete permission.

## Open questions

[Q57](../00-product/open-questions.md#q57): retention/erasure interaction. Audit durability is resolved by accepted security requirements and ADR-13.

## Related documents

[Role policy](../01-domain/roles-and-permissions.md) · [Security](../06-architecture/security-and-identity.md) · [Invariants](../06-architecture/implementation-invariants.md)
