# Entity: User

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Identity & Organization\
> **Updated:** 2026-09-17

## Purpose

A person/account within exactly one Organization. Account roles are a set; the current Learner membership is the Learner entitlement. Operational grants scope participation independently.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| name | text, required | Display name |
| email | normalized text, required | Organization-scoped login/contact identity |
| roles | enum set, required | Zero or more of the six canonical account roles; no implicit role |
| status | enum, required | pending, active, deactivated |
| security_version | integer/version, required | Authoritative session invalidation version |
| created_at / updated_at | timestamps, required | Account lifecycle times |
| deleted_at | timestamp, optional | Soft deletion; must also disable access |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

`pending → active → deactivated → active`. Pending users accept a purpose-bound invitation before becoming active. Credential/token/session storage belongs to the identity implementation, not plaintext fields in this model.

- Unique normalized `(organization_id, email)`; invitation retries/resends do not create another account.
- Protected access requires active, not deleted, with current role/grant checks.
- Activation, reactivation, adding Learner while active, and restoration that increases consumption obey the atomic [capacity rule](../01-domain/core-concepts.md#learner-capacity).
- Deactivation/removing Learner releases capacity and denies affected access without deleting enrollments, progress, submissions, authored posts or certificates.
- Empty role set confers no resource access. Profile edits never change roles, organization, status or security state.
- Protect privileged targets and final usable Administrator through dedicated operations. Status/role/credential changes invalidate sessions; mass-update paths cannot bypass those checks.
- Authorship and staff grants do not automatically supply missing account roles. Deactivation is not GDPR erasure.

## Open questions

[Q57](../00-product/open-questions.md#q57): retention/erasure. Ordinary lifecycle and multi-role behavior are resolved.

## Related documents

[Organization](organization.md) · [Enrollment](enrollment.md) · [Role policy](../01-domain/roles-and-permissions.md) · [Provisioning](../02-features/user-and-seat-provisioning.md)
