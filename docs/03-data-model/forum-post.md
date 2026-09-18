# Entity: Forum Post

> **Status:** Confirmed domain baseline; linked feature gates remain\
> **Authority:** Canonical — entity representation\
> **Owner module:** Discussion\
> **Updated:** 2026-09-17

## Purpose

Markdown question body or nested reply within a Forum Thread.

## Key fields

| Field | Conceptual type / requirement | Meaning |
|---|---|---|
| id | identifier, required | Stable record identity |
| organization_id | reference, required | Owning Organization |
| thread_id | reference, required | Containing Thread |
| parent_post_id | reference, optional | Reply parent within the same Thread |
| author_id | reference, required | Author |
| content | Markdown, required | Non-empty sanitized content |
| created_at / edited_at | timestamps, required / optional | Posting/edit times |
| deleted_at | optional timestamp | Removal tombstone, not orphaning descendants |

## Organization and integrity

All referenced records must belong to the same organization. Apply the [model conventions](README.md#shared-constraints) and the additional course/cohort/run constraints below. Field types describe the conceptual contract; concrete SQL types and indexes are implementation work.

## Authorization and audit

Use the single [role/grant policy](../01-domain/roles-and-permissions.md); this document does not create a competing CRUD permission matrix. Mutations are owner-module operations, never arbitrary cross-module entity updates. Required sensitive-action audit commits in the same transaction ([ADR-13](../06-architecture/decisions.md#adr-13--atomic-state-and-durable-effects)).

## Lifecycle, relationships and constraints

Created → optionally edited or tombstoned. Parent must belong to the same thread; no cycles. Thread/cohort authorization applies to every read/edit, not only top-level thread retrieval. Own-edit/delete and moderation permissions follow the central matrix, including its Manager restriction. Moderation of others' content is audited. Do not enable uploads or arbitrary embedded HTML.

## Open questions

[Q57](../00-product/open-questions.md#q57): forum retention/erasure; [Q69](../00-product/open-questions.md#q69): former-member access.

## Related documents

[Forum Thread](forum-thread.md) · [User](user.md) · [Forum feature](../02-features/course-and-module-forums.md)
