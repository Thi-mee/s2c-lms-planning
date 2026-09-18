# Entity: Quiz Question

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Assessment\
> **Updated:** 2026-09-17

## Purpose

One weighted question owned by a Quiz.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| quiz_id | reference, required | Parent Quiz |
| type | enum, required | single_choice, multi_select, true_false |
| prompt_text | text, required | Question prompt |
| points | nonnegative numeric value, required | Available score; representation supports approved scoring precision |
| position | integer, required | Question order within Quiz |
| created_at / updated_at | timestamps, required | Edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Has ordered Quiz Options. Validate single-choice has exactly one correct answer; true/false has exactly two choices with exactly one correct answer; multi-select has at least one correct choice. The Quiz must have positive total points before publication. Version definition changes and preserve submitted snapshots. Learner delivery excludes correct-option flags; post-submission feedback follows Q73. No standalone question-bank scope.

## Open questions

[Q71](../00-product/open-questions.md#q71): partial-credit rule. [Q74](../00-product/open-questions.md#q74): live edits.

## Related documents

[Quiz](quiz.md) · [Quiz Option](quiz-option.md) · [Quiz Submission Answer](quiz-submission-answer.md)
