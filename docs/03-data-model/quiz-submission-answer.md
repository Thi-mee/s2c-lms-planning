# Entity: Quiz Submission Answer

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Assessment\
> **Updated:** 2026-09-17

## Purpose

Immutable per-question response and grading evidence within a submitted attempt.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| submission_id | reference, required | Parent submission/run |
| question_id | reference, required | Question identity represented by snapshot |
| selected_option_ids | reference set, required | Chosen options, possibly empty for unanswered |
| question_and_options_snapshot | structured data, required | Prompt, available points, choices and scoring key/version seen for this evidence |
| is_correct / points_awarded | boolean / numeric, required | Server-computed, frozen outcome |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Exactly one answer per `(organization_id, submission_id, question_id)` for each scored question. Question belongs to the submitted quiz definition and selected options belong to that question. Preserve snapshots if original authoring content changes; restrict original-row deletion or retain tombstones. Do not allow client-created scores. Internal snapshots may contain answer keys; public review DTOs must enforce Q73 and never serialize the internal record wholesale. Available/awarded points obey the approved grading rule.

## Open questions

[Q71](../00-product/open-questions.md#q71): partial credit; [Q73](../00-product/open-questions.md#q73): review; [Q57](../00-product/open-questions.md#q57): retention.

## Related documents

[Quiz Submission](quiz-submission.md) · [Quiz Question](quiz-question.md) · [Quiz Option](quiz-option.md)
