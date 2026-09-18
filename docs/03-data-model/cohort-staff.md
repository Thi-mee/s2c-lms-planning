# Entity: Cohort Staff

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Enrollment & Cohorts\
> **Updated:** 2026-09-17

## Purpose

Resource-scoped Coordinator or Facilitator assignment; not an account-role grant.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| cohort_id | reference, required | Staffed Cohort |
| user_id | reference, required | Eligible User |
| capability | enum, required | coordinator or facilitator |
| created_at | timestamp, required | Assignment time |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Assign/revoke with audit. Unique `(organization_id, cohort_id, user_id, capability)`. The user must hold the corresponding account role for assignment and effective authorization. One person can hold both capabilities; these remain distinct permissions. Coordinators assign only within their own cohort; Managers/Admins operate within their organization. Preserve the required Coordinator staffing invariant on cohort scheduling and grant changes.

## Open questions

Coordinator/Facilitator distinction is settled; no further role-split question.

## Related documents

[Cohort](cohort.md) · [User](user.md) · [Role policy](../01-domain/roles-and-permissions.md)
