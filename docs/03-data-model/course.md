# Entity: Course

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Course Authoring\
> **Updated:** 2026-09-17

## Purpose

Reusable course content delivered through Cohorts; course identity is stable across learning runs.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| title | text, required | Course title |
| description / metadata | text / JSON, optional | Description and bounded descriptive metadata |
| status | enum, required | draft, published, archived |
| requirements_version | version, required | Changes when completion-bearing requirements change; not a full course-versioning product |
| created_at / updated_at | timestamps, required | Creation/edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

`draft → published → archived`. Has ordered Modules, Cohorts, one Course Author owner, and user/course Certificates.

- Exactly one owner via `course_authors` for MVP; no scalar owner field competing with that grant.
- Publish directly through an owning Course Author/Admin action; no approval gate. Only published courses launch cohorts.
- Learners see assigned content, never all published courses merely because status is published. Staff library visibility serves cohort launch.
- Validate that published content has completion-bearing requirements and valid quizzes. Prevent deletion from cascading to enrollment/attempt/certificate evidence.
- Reordering is visible to active learners. Required-flag changes recompute incomplete runs; completed snapshots remain intact.
- Course selling, hard prerequisites and course-version authoring workflows are not current MVP requirements.

## Open questions

[Q69](../00-product/open-questions.md#q69): archive with active cohorts. [Q74](../00-product/open-questions.md#q74): live structural/assessment edits.

## Related documents

[Course Author](course-author.md) · [Module](module.md) · [Cohort](cohort.md) · [Administration](../02-features/course-and-module-administration.md)
