# Entity: Forum Thread

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Discussion\
> **Updated:** 2026-09-17

## Purpose

Discussion topic within one cohort at course or module scope.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| cohort_id | reference, required | Visibility group |
| scope | enum, required | course or module |
| scope_id | reference, required | Course or Module matching the cohort’s Course |
| title | text, required | Thread title |
| author_id | reference, required | Creator |
| status | enum, required | open, resolved, locked |
| created_at / updated_at | timestamps, required | Thread times |
| deleted_at | optional timestamp | Moderation tombstone preserving replies/history |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

`open → resolved` or `open → locked`; controlled reopening/state combinations are specified during the discussion slice. The cohort and referenced course/module must match, not merely share an organization. Posts belong to this thread, including its initial question body.

Use the canonical role policy for visibility/posting/moderation, including Facilitators and organization-scoped Manager read/post access. Learners cannot see another cohort just because it runs the same course. Moving/re-enrolling a learner does not move threads. Sanitize Markdown and preserve tombstones/reply integrity on moderation removal. Public/cross-cohort forums and attachments are excluded.

## Open questions

[Q69](../00-product/open-questions.md#q69): access after end/drop; [Q57](../00-product/open-questions.md#q57): retention. Thread-state UI details are local engineering choices unless they change visibility/ownership.

## Related documents

[Forum Post](forum-post.md) · [Cohort](cohort.md) · [Forum feature](../02-features/course-and-module-forums.md)
