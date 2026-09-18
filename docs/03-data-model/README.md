# Canonical data model

> **Status:** Implementation baseline; conceptual contracts, not migrations\
> **Authority:** Canonical — shared model constraints and entity index\
> **Updated:** 2026-09-17

## Purpose

Represent the accepted domain with the smallest useful model. SQL types, physical schemas, indexes and ORM mapping are engineering work. The [core concepts](../01-domain/core-concepts.md) own behavior and [roles](../01-domain/roles-and-permissions.md) own permissions; entity pages define representation and integrity constraints.

## Shared constraints

- Organization is the ownership root; every organization-owned record carries organization_id. Composite references must preserve organization and relevant course/cohort/run relationships. Do not accept parent IDs based only on their existence.
- Enrollment is the Learning Run. Raw progress and attempts reference it. A user/course summary is a query, not an alternative write model.
- Write through owner-module interfaces. Efficient reviewed integration reads and cross-module relational constraints are allowed.
- Submitted attempts, completion evidence and issued display snapshots preserve point-in-time meaning. No ordinary authoring/account deletion cascades into history. Retention/erasure follows an explicit product policy.
- Use database uniqueness plus transactional serialization for capacity, active-run exclusivity, allowance and credential issuance; UI checks are insufficient.
- Authorization tables are not duplicated here. Each entity refers to the central matrix; fields required only in particular lifecycle states are explicitly conditional.

## Entity index

| Module | Entities |
|---|---|
| Identity & Organization | [Organization](organization.md), [User](user.md), [Audit Log](audit-log.md) |
| Course Authoring | [Course](course.md), [Course Author grant](course-author.md), [Module](module.md), [Lesson](lesson.md) |
| Enrollment & Cohorts | [Cohort](cohort.md), [Cohort Staff](cohort-staff.md), [Enrollment / Learning Run](enrollment.md) |
| Assessment | [Quiz](quiz.md), [Question](quiz-question.md), [Option](quiz-option.md), [Submission and allowance reset](quiz-submission.md), [Submission Answer](quiz-submission-answer.md) |
| Progress & Certification | [Progress](progress.md), [Certificate](certificate.md); completion state/evidence stored on Enrollment via its owner |
| Discussion | [Forum Thread](forum-thread.md), [Forum Post](forum-post.md) |
| Notifications | [Notification / durable email intent](notification.md) |
| Licensing | [Versioned license contract](../04-api-design/license-contract.md); verified projection linked to Organization, no billing entity |
| Deferred | [Media Asset](media-asset.md) |

Auth sessions/invites, durable-work claims and allowance/reset records are supporting persistence needed by actual flows; they do not imply separate product platforms. No Course Achievement entity, generic entitlement engine, event-sourcing platform or service-specific database is required.

## Domain relationships

```text
Organization
 ├─ User (roles, lifecycle)
 └─ Course ─ Module ─ Lesson ─ Quiz
      └─ Cohort ─ Cohort Staff
           ├─ Forum Thread ─ Forum Post
           └─ Enrollment (User + Cohort + Course; the Learning Run)
                ├─ Progress (Lesson)
                ├─ Quiz Submission ─ Quiz Submission Answer
                └─ completion time + evidence snapshot

Certificate (unique Organization + User + Course)
 └─ source_enrollment_id: first qualifying issuance run
```

## Open questions

Feature gates are linked from their entities to [open questions](../00-product/open-questions.md). Do not convert a gated field/workflow into an arbitrary product default.

## Related documents

[Module ownership](../06-architecture/module-boundaries.md) · [Invariants](../06-architecture/implementation-invariants.md) · [Entity template](entity-template.md) · [Historical sketch](initial-entities.md)
