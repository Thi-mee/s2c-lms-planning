# Entity: Quiz

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Assessment\
> **Updated:** 2026-09-17

## Purpose

Assessment definition associated with one Lesson; Course Authoring composes it through Assessment’s public authoring interface.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| lesson_id | reference, required | Lesson; at most one quiz per lesson |
| title | text, required | Quiz title |
| type | enum, required | practice or graded |
| passing_score_percentage | decimal, required for graded | Threshold from 0 to 100 |
| max_attempts | positive integer for graded | Per-run allowance; null for practice |
| definition_version | version, required | Identifies content/scoring definition |
| created_at / updated_at | timestamps, required | Edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

No independent publishing state; availability follows parent course and run access. Unique organization/lesson relationship for current cardinality. Practice has unlimited attempts and no completion impact. Graded quizzes require a threshold and positive allowance.

Questions have nonnegative points; a published scorable quiz needs at least one positive-point question and valid options. The score is computed server-side from earned/available points, not client values. Preserve submitted definition/answer snapshots across edits. Do not physically cascade-delete referenced history. Reset records restore allowance for a run/quiz without rewriting submissions.

## Open questions

[Q71](../00-product/open-questions.md#q71): multi-select scoring. [Q72](../00-product/open-questions.md#q72): attempt lifecycle. [Q73](../00-product/open-questions.md#q73): feedback/test. [Q74](../00-product/open-questions.md#q74): edits during attempts.

## Related documents

[Lesson](lesson.md) · [Quiz Question](quiz-question.md) · [Quiz Submission](quiz-submission.md) · [Quiz feature](../02-features/quiz-engine.md)
