# Entity: Certificate

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Progress & Certification\
> **Updated:** 2026-09-17

## Purpose

User/course credential created from a qualifying completed run, distinct from raw enrollment completion history.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| user_id / course_id | references, required | Credential subject and Course |
| source_enrollment_id | reference, required | Completed run that first caused issuance; must match user/course/org |
| verification_id | opaque cryptographically random identifier, required | Unique authenticated lookup identity, not a digital signature |
| status | enum, required | issued or void; replacement semantics Q75 |
| display_snapshot | structured data, required | Learner/course/org display names, signature line, template version and stable branding references |
| issued_at | timestamp, required | Issuance time |
| voided_at / void_reason | timestamp / text, conditional | Administrative audit evidence; never destructive deletion |
| pdf_storage_key | optional reference | If retained; render from frozen data when generated on demand |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Automatic issuance is unique per `(organization_id, user_id, course_id)`, **not enrollment**. Keep `source_enrollment_id` immutable. A later completed run records its own completion but does not automatically issue another credential. Duplicate completion processing returns the existing credential.

Issued display data is frozen; content/user/branding changes, requirement changes or attempt reset do not silently change validity. Public verification pages and signed PDFs are not MVP commitments. Authorized lookup can validate the ID against the credential status; a random ID alone is not proof offline.

Preserve a voided record and audit the actor/reason. Administrative reissue is an intended restricted feature but Q75 must define replacement identity/history before implementing it. Do not loosen automatic uniqueness or rewrite the original certificate to guess that answer. Ordinary deactivation does not erase the credential. Named snapshot retention/erasure remains Q57.

## Open questions

[Q75](../00-product/open-questions.md#q75): reissue representation and ID. [Q57](../00-product/open-questions.md#q57): erasure of credential snapshots.

## Related documents

[Enrollment](enrollment.md) · [User](user.md) · [Course](course.md) · [Completion feature](../02-features/completion-and-certificate-generator.md)
