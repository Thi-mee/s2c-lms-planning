# Entity: Progress

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Progress & Certification\
> **Updated:** 2026-09-17

## Purpose

Lesson completion evidence owned by one Enrollment. User/course percentages are derived summaries, never the source of raw history.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| enrollment_id | reference, required | Owning Learning Run |
| lesson_id | reference, required | Lesson in that run’s Course |
| completed | boolean, required | Recorded completion state |
| completed_at | timestamp, when completed | Completion evidence time |
| lesson_content_version / completion_basis | version / evidence, when completed | Content seen and accepted completion trigger |
| created_at / updated_at | timestamps, required | Record times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Unique `(organization_id, enrollment_id, lesson_id)`. The lesson's module/course must match Enrollment.course_id. A user_id may be derived through enrollment; it must not replace enrollment identity in a uniqueness key.

Incomplete → completed, idempotently. The trigger is gated by Q70. Account deactivation or new enrollment does not reset/delete this record. Text edits do not erase recorded completion. Aggregate against the current requirements only for incomplete runs; completed runs retain their recorded evaluation. Practice attempts never become graded-quiz completion evidence. Do not allow a browser to mark another run's lesson or directly set course completion.

## Open questions

[Q70](../00-product/open-questions.md#q70): reading trigger. [Q57](../00-product/open-questions.md#q57): retention.

## Related documents

[Enrollment](enrollment.md) · [Lesson](lesson.md) · [Completion feature](../02-features/completion-and-certificate-generator.md)
