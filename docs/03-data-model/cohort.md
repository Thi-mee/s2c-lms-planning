# Entity: Cohort

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Enrollment & Cohorts\
> **Updated:** 2026-09-17

## Purpose

One scheduled delivery group for one reusable Course; scopes staff and discussion.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| course_id | reference, required | Published Course at launch |
| title | text, required | Cohort title |
| start_date / end_date | timestamps, required | UTC schedule, end no earlier than start |
| created_at / updated_at | timestamps, required | Edit times |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

`upcoming → active → ended` follows the schedule. A scheduled cohort needs eligible Coordinator staffing before it can be active; create/schedule and initial staff assignment atomically so crossing the start time cannot activate an unstaffed cohort. Enforce staffing validity when changing schedule or grants.

Has Cohort Staff and Enrollments; each Thread belongs to this cohort. Staff grants may include multiple Coordinators/Facilitators and both capabilities for one user. Per-module scheduling is deferred. A cohort ending does not mark every enrollment completed. A learner's run completion is evaluated from evidence. Historical runs/threads cannot be cascade-deleted through cohort removal.

## Open questions

[Q69](../00-product/open-questions.md#q69): access outside active window, withdrawal and archival.

## Related documents

[Cohort Staff](cohort-staff.md) · [Enrollment](enrollment.md) · [Core concepts](../01-domain/core-concepts.md#cohort-lifecycle)
