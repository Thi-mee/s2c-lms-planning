# Entity: Quiz Submission

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Assessment\
> **Updated:** 2026-09-17

## Purpose

One enrollment-owned attempt; submitted state is immutable evidence and counts only for its own run.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| enrollment_id | reference, required | Owning Learning Run |
| quiz_id | reference, required | Quiz in the run’s Course |
| allowance_epoch | integer, required | 0 initially; changes after audited reset |
| attempt_number | positive integer, required | Sequence within this run/quiz/allowance epoch |
| state | enum, required | in_progress or submitted; Q72 gates reservation/timing semantics |
| quiz_definition_snapshot | version + content/scoring snapshot, required at submission | Definition and threshold used for scoring |
| earned_points / available_points | numeric, when submitted | Preserve exact grading calculation inputs |
| score_percentage / passed | decimal / boolean, when submitted | Server-computed outcome; presentation rounding never changes pass comparison |
| started_at / submitted_at | timestamps, conditional | Attempt lifecycle evidence |
| request_key | identifier, required for submission | Deduplicates retries within organization/run/quiz |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Unique `(organization_id, enrollment_id, quiz_id, allowance_epoch, attempt_number)` and scoped request key. All questions/options must match the quiz and all learning evidence must match the enrollment course.

Submitted attempts/answers cannot be edited. Keep scores and content snapshots; references alone are insufficient. Practice attempts never contribute to completion. For graded completion, any passing submission belonging to this run is sufficient; do not borrow another run's pass.

Validation of in-progress fields is state-dependent: a draft attempt does not yet require a score or submitted_at. Starting/reservation/timeout policy remains Q72; do not encode it accidentally through required-field defaults. Staff previews are separate from earned attempts and remain Q73.

## Allowance reset without history deletion

Maintain a small Assessment-owned allowance record keyed by organization/enrollment/quiz, with current epoch and serialized reservation/submission accounting. A permitted reset after exhausted failed graded attempts starts a new allowance epoch with the configured max_attempts; record actor, reason, time, previous/new epoch and allowance in append-only reset evidence plus audit. This supporting record is not a new product-level entity or a generic entitlement engine.

Attempt numbers may begin at 1 in the new epoch; historical attempts and their unique identities remain unchanged. Limit validation applies within the applicable allowance, not against all lifetime attempt rows. Concurrent reset/submission is serialized; retrying the same reset request does not grant another allowance. Re-enrollment has its own independent allowance starting at epoch 0. Reset does not revoke an existing certificate or rewrite completed-run evidence.

## Open questions

[Q72](../00-product/open-questions.md#q72): consumption/resume/timing. [Q73](../00-product/open-questions.md#q73): review/tests. [Q74](../00-product/open-questions.md#q74): edits mid-attempt.

## Related documents

[Enrollment](enrollment.md) · [Quiz](quiz.md) · [Quiz Submission Answer](quiz-submission-answer.md) · [Audit](audit-log.md)
