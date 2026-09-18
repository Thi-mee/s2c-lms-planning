# Entity: Course Author grant

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Course Authoring\
> **Updated:** 2026-09-17

## Purpose

The course-scoped authorship join; distinct from the Course Author account role.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| course_id | reference, required | Owned Course |
| user_id | reference, required | Eligible Course Author or Administrator |
| author_role | enum, required | owner in MVP; collaborator is not enabled |
| created_at | timestamp, required | Grant time |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Grant or atomically replace ownership; audit changes. Unique `(organization_id, course_id, user_id)` and exactly one owner per course. Never leave a committed course ownerless. Resource assignment does not confer the account role. The account role plus grant is required for ordinary author edits; Administrator override is organization-scoped.

Use the resource-grant matrix for who can assign/transfer ownership, including Organization Managers assigning an eligible author. Multi-author collaboration remains deferred; retaining a join does not implement collaboration permissions.

## Open questions

No ownership blocker. Collaboration is outside MVP.

## Related documents

[Course](course.md) · [User](user.md) · [Role policy](../01-domain/roles-and-permissions.md#resource-grant-authority)
