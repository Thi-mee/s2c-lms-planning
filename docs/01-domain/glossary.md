# Glossary

> **Status:** Confirmed baseline\
> **Authority:** Canonical — terminology\
> **Updated:** 2026-09-17

## Purpose

Use these terms consistently in product, API, UI and implementation guidance.

| Term | Meaning |
|---|---|
| Variable | Company/vendor publishing the product; separate from customer organizations |
| Variable LMS | Product name |
| s2c | First customer organization, using the product for internal training |
| Organization | Ownership/security boundary for accounts and resources; one customer organization per initial installation |
| User / Account | A person's identity within one organization, with lifecycle state and a set of account roles |
| Account Role | Eligibility/authority held at organization-account level; not a numeric rank |
| Resource Grant | Assignment of a role/capability to a particular course or cohort; never a privileged account-role grant |
| Learner | Account role/entitlement permitting learning through assigned enrollments |
| Learner Entitlement | In MVP, membership in the Learner account-role set; the predicate used for licensed learner capacity |
| Active Learner | Unique account with status active, not deleted, and currently holding the Learner entitlement |
| Learner Seat | One unit consumed by an Active Learner; staff-only, pending and deactivated accounts consume zero |
| Course Author | Account role for content creation; course_authors supplies course scope |
| Administrator | Privileged organizational security role; does not imply cross-organization or operator access |
| Organization Manager | Organizational user/operational-role manager; cannot grant/revoke Administrator or Organization Manager |
| Course | Reusable authored content organized into modules and lessons; draft, published or archived |
| Module | Ordered grouping of lessons within a course |
| Lesson | Ordered unit of rich-text notes and/or external readings, with an explicit required flag and optional quiz |
| Cohort | Scheduled delivery group for one published course, with cohort-scoped staff and discussions |
| Cohort Coordinator | Account role plus cohort grant for delivery logistics and membership |
| Learning Facilitator | Account role plus cohort grant for learner support and moderation |
| Cohort Staff | Coordinator and/or Facilitator assignments; one person may hold both |
| Enrollment / Learning Run | One learner's participation in one cohort/course run. Enrollment is the stored entity; Learning Run describes its meaning, not a second entity |
| Re-enrollment | A new Enrollment with fresh progress, attempt allowance and completion state; prior runs remain historical |
| Progress | Lesson completion evidence belonging to an Enrollment, not globally to user + lesson |
| Assessment / Quiz | Practice or graded quiz; MVP question types are single-choice, multi-select and true/false |
| Quiz Submission / Attempt | A learner's assessment attempt belonging to an Enrollment and Quiz; submitted evidence is immutable |
| Completion Evaluation | Evaluation of one Enrollment's lesson/quiz evidence against course requirements; no separate entity is required initially |
| Certificate | User/course credential issued automatically from a qualifying completed Enrollment; distinct from the Enrollment's completion record |
| Course Achievement | Optional derived user/course summary over historical runs; no separate persisted entity required for MVP |
| Vendor Control Plane | Variable-operated issuance/billing/customer commercial system outside the LMS |
| Deployment Package | Versioned installation/upgrade artifacts referencing exact application images and compatibility requirements |

## Retired terms

`Instructor` → Course Author. `Learning Coordinator` → Cohort Coordinator and/or Learning Facilitator. `Student` → Learner. `S2C LMS` → Variable LMS. `max_active_seats`/ambiguous seat counters → `max_active_learners` for the current entitlement. Historical snapshots retain original names solely for context.

## Open questions

None in foundational terminology. New product meanings require a glossary update alongside the owning rule.

## Related documents

[Core concepts](core-concepts.md) · [Roles](roles-and-permissions.md) · [Accepted decisions](../00-product/accepted-decisions.md)
