# Entity: Quiz Option

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Assessment\
> **Updated:** 2026-09-17

## Purpose

Answer choice for one Quiz Question, including server-only grading information.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| question_id | reference, required | Parent Question |
| option_text | text, required | Visible choice text |
| is_correct | boolean, required | Server-side grading key |
| position | integer, required | Display order within Question |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

At least two choices per question; valid correctness counts follow question type. Options selected in a submission must belong to that question and that submitted quiz version. Correctness is never included in the taking-quiz DTO. Preserve option text/key snapshots in submission evidence so later author edits do not change what an old response means.

## Open questions

[Q73](../00-product/open-questions.md#q73): learner review visibility.

## Related documents

[Quiz Question](quiz-question.md) · [Quiz Submission Answer](quiz-submission-answer.md)
