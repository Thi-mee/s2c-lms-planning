# Entity: Lesson

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Course Authoring\
> **Updated:** 2026-09-17

## Purpose

Ordered text learning unit belonging to one Module; includes notes/readings and may have one associated Quiz.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| module_id | reference, required | Parent Module |
| title | text, required | Lesson title |
| position | integer, required | Unique within Module |
| required | boolean, required | Counts toward completion when true; explicitly selected/defaulted in authoring |
| lesson_notes | rich text, optional | Sanitized authored content |
| required_readings | JSON list, optional | Title/description and allowed external URL entries |
| content_version | version, required | Identifies displayed evidence when completion is recorded |
| created_at / updated_at | timestamps, required | Edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Visibility follows course and run authorization. Unique `(organization_id, module_id, position)`. A required-reading list is content, not independently tracked reading entities. At most one Quiz per Lesson in the current model.

Text edits do not erase recorded completion; required-flag changes update course requirements version and incomplete-run percentages. A Progress record belongs to an enrollment and references this lesson; no global user/lesson completion key. Linear order does not gate navigation. Deleting/moving lessons must not corrupt historical references/snapshots. Sanitize rich text and reject executable URLs.

## Open questions

[Q70](../00-product/open-questions.md#q70): what marks reading complete. [Q74](../00-product/open-questions.md#q74): live structural changes.

## Related documents

[Module](module.md) · [Quiz](quiz.md) · [Progress](progress.md) · [Reading feature](../02-features/rich-text-and-reading-lessons.md)
