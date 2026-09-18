# Feature: Course and module forums

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Discussion; Notifications\
> **Updated:** 2026-09-17

## Purpose and users

Learners and staff discuss training within a cohort, in course-wide or module-specific threads.

## Goal and non-goals

Create threads, reply, resolve/lock/moderate, and email thread authors about replies. Markdown only; no attachments, public/cross-cohort forums, global notification center or speculative reaction system.

## Core flows

1. Authorized participant creates a thread and initial post in a cohort's course or module scope. Validate that the scoped course/module belongs to the same course as the cohort.
2. Authorized participants reply. Persist the post and deduplicated reply email intent together; do not email an actor about their own reply.
3. Authorized staff resolve/lock or remove content. Preserve referential structure using deletion tombstones; record moderation of others' content in the business transaction's audit.

## Business rules

Every thread and post belongs to an organization and a single cohort. Current association with a different cohort does not grant access to an old thread. A future transfer cannot move discussions accidentally. Sanitize Markdown output, disallow unsafe HTML/URLs and authorize linked resources independently. Locked threads reject new replies. Email delivery uses durable retries and current recipient/access checks; do not place unnecessary discussion content in email.

## Permissions

The [canonical matrix](../01-domain/roles-and-permissions.md) is the single source for posting, own edits and moderation. Preserve its existing Organization Manager own-edit restriction until deliberately changed; owning a post alone does not bypass policy. Staff authority is course/cohort scoped.

## Data requirements

[Thread](../03-data-model/forum-thread.md), [Post](../03-data-model/forum-post.md), [Cohort](../03-data-model/cohort.md), [Notification](../03-data-model/notification.md), [Audit](../03-data-model/audit-log.md).

## Acceptance and failure cases

Reject another cohort's thread/module IDs even within the same organization. Thread/post deletion cannot orphan replies. Concurrent reply/lock follows a serialized permission check. SMTP outage retains committed discussion and retry intent. Sanitizer and authorization tests cover crafted Markdown, stale grants and deleted/inactive accounts.

## Open questions

[Q69](../00-product/open-questions.md#q69): ended/dropped cohort access. [Q57](../00-product/open-questions.md#q57): content retention. Rich text attachments and a notification center remain deferred, not active questions.

## Related documents

[Roles](../01-domain/roles-and-permissions.md) · [Navigation](../05-frontend/navigation.md) · [Module boundaries](../06-architecture/module-boundaries.md)
