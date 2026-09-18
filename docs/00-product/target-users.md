# Target users

> **Status:** Confirmed baseline\
> **Authority:** Canonical — actors\
> **Updated:** 2026-09-17

## Purpose

Identify Variable LMS actors without duplicating the permission matrix. One person may hold multiple account roles and resource grants.

| Actor | Responsibility |
|---|---|
| Learner | Takes assigned courses through enrollments; reads, participates, takes quizzes, views own progress and credentials |
| Course Author | Creates reusable course content and assessments; authorship is course-scoped |
| Cohort Coordinator | Launches/configures cohorts, assigns learners and oversees delivery; cohort-scoped |
| Learning Facilitator | Supports learners, moderates discussions and monitors progress; cohort-scoped |
| Organization Manager | Manages ordinary organizational users/operational roles, resource assignments, branding and capacity visibility; cannot delegate privileged security roles |
| Administrator | Manages organizational security and system settings through explicitly authorized actions; no cross-customer authority |
| Installation operator | Controls deployment, initial bootstrap, backups and recovery; host access is outside the application role model |
| Variable vendor system | Issues signed licenses and owns commercial workflows outside the LMS |

s2c is the first customer organization. Organization Managers do not issue licenses, edit signed capacity or process subscriptions inside the LMS. Staff need the Learner entitlement and their own enrollment when participating as learners.

## Open questions

No unresolved role-set or Coordinator/Facilitator naming decision. Specific preview/review behavior is [Q73](open-questions.md#q73); commercial future roles are outside current scope.

## Related documents

[Role authority](../01-domain/roles-and-permissions.md) · [Glossary](../01-domain/glossary.md) · [Journeys](../01-domain/user-journeys.md)
