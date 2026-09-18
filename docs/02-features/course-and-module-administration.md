# Feature: Course and module administration

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Course Authoring\
> **Updated:** 2026-09-17

## Purpose and users

Course Authors and Administrators organize and publish text-based training. Each course has one organization and one owning account (Course Author or Administrator) for MVP.

## Goal and non-goals

Create a draft course, order modules and lessons, publish directly, and retain historical records when content is retired. No approval queue, collaborator editing, public course catalog or self-enrollment is included.

## Core flows

1. Create course and its single owner grant atomically. An Administrator creating on another author's behalf supplies an eligible same-organization owner.
2. Add and reorder modules/lessons; author content through [lessons](rich-text-and-reading-lessons.md) and [quizzes](quiz-engine.md).
3. Validate structure and publish directly. Publication permits assignment and authorized learning access; it does not make the course public or enroll anyone.
4. Transfer ownership through the explicit grant operation. Archive behavior affecting active cohorts is gated by Q69.

## Business rules

Course states are `draft`, `published`, `archived`. Modules/lessons have stable IDs and explicit positions. Exactly one owner is preserved through transfer. A Manager can assign an eligible owner but cannot author through the Manager role alone.

**Engineering validation default:** reject publication without learnable content or with a zero completion denominator, malformed quiz settings, inconsistent organization references or missing owner. Return actionable validation errors; do not silently invent a completion outcome for an empty course. Completion requirements and their version are exposed through Authoring's public contract.

Pure ordering changes do not rewrite learning history. Text edits preserve recorded completion; changing required flags recomputes incomplete runs. Other live-definition/in-flight edits require Q74 before implementation. Never cascade-delete historical answers, progress or certificates when authoring data changes.

## Permissions

[Canonical permissions](../01-domain/roles-and-permissions.md) define owner-scoped authoring, Administrator actions and resource grants. A Course Author account role by itself does not permit editing another author's course.

## Data requirements

[Course](../03-data-model/course.md), [Course Author grant](../03-data-model/course-author.md), [Module](../03-data-model/module.md), [Lesson](../03-data-model/lesson.md), [Audit Log](../03-data-model/audit-log.md).

## Acceptance and failure cases

Publish an owned valid draft without approval; reject another author's edit and cross-organization owner assignment. Concurrent transfers preserve one owner; reorder preserves unique positions. Archived or edited definitions must not erase previously submitted evidence or issued display snapshots.

## Open questions

[Q69](../00-product/open-questions.md#q69): archive/access boundaries. [Q74](../00-product/open-questions.md#q74): live-definition editing policy.

## Related documents

[Core concepts](../01-domain/core-concepts.md) · [Cohort management](cohort-and-enrollment-management.md) · [Navigation](../05-frontend/navigation.md)
