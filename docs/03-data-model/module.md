# Entity: Module

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Course Authoring\
> **Updated:** 2026-09-17

## Purpose

Ordered grouping of Lessons within exactly one Course.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| course_id | reference, required | Parent Course |
| title | text, required | Module title |
| description | text, optional | Summary/objectives |
| position | integer, required | Unique order within Course |
| created_at / updated_at | timestamps, required | Edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

No independent publication lifecycle; learner visibility follows authorized course/cohort access. Modules contain Lessons and may be a forum scope. Unique `(organization_id, course_id, position)` with atomic reordering. Prevent incompatible parent moves/deletion when dependent learning history exists. There is no module drip schedule or hard prerequisite graph in MVP.

## Open questions

[Q74](../00-product/open-questions.md#q74): structural changes to live courses.

## Related documents

[Course](course.md) · [Lesson](lesson.md) · [Forum Thread](forum-thread.md)
