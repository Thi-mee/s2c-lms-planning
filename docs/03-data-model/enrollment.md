# Entity: Enrollment / Learning Run

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Enrollment & Cohorts\
> **Updated:** 2026-09-17

## Purpose

Authoritative identity for a learner’s participation and learning evidence in a cohort. A Learning Run is this entity, not an additional abstraction.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| user_id | reference, required | Learner account |
| cohort_id | reference, required | Cohort of this run |
| course_id | reference, required | Must equal Cohort.course_id; explicit for run uniqueness/integrity |
| status | enum, required | enrolled, in_progress, completed, dropped |
| enrolled_at | timestamp, required | Assignment time |
| started_at / completed_at / dropped_at | timestamps, conditional | State transition evidence |
| completion_snapshot | JSON/evidence, when completed | Requirements version and IDs/results used by evaluation |
| updated_at | timestamp, required | Last state transition |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

`enrolled → in_progress → completed`; enrolled/in_progress may become dropped through the authorized workflow. Historical completed/dropped rows are not reused. A new genuine enrollment has a new ID, fresh progress, quiz allowance and lifecycle. Keep at most one active run (`enrolled`, `in_progress`) per `(organization_id, user_id, course_id)` using a constraint/serialized mutation.

- Creation requires an active Learner-entitled account, authorized staff and a cohort/course in the same organization. Enrollment consumes no additional licensed seat.
- First recorded activity begins the run; completion is set only by the completion coordinator through this module's interface.
- Has many Progress and Quiz Submission records. Certificate may reference this run as first issuance source, but is not one-per-run.
- Deactivation retains the row and learning records; it changes account access, not historical outcomes.
- No generic `cohort_id` update or progress carry-forward. Transfer is gated; re-enrollment is defined and starts fresh.
- Ordinary deletion must not erase learning history; retention erasure is a separately approved operation.

## Open questions

[Q69](../00-product/open-questions.md#q69): withdrawal/access. [Q79](../00-product/open-questions.md#q79): transfer if required. [Q57](../00-product/open-questions.md#q57): erasure.

## Related documents

[User](user.md) · [Cohort](cohort.md) · [Progress](progress.md) · [Quiz Submission](quiz-submission.md) · [Certificate](certificate.md)
