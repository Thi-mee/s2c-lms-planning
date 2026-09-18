# Feature: Cohort and enrollment management

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Enrollment & Cohorts\
> **Updated:** 2026-09-18

## Purpose and users

Coordinators, Organization Managers and Administrators assign active Learners to scheduled deliveries of a published course. This is the MVP's entry into learning.

## Goal and non-goals

**Implementation status (2026-09-18):** The scheduling/staffing path is implemented: published-course selection, UTC schedule, atomic initial Coordinator, optional Facilitator, current scoped staff views, explicit staff changes and Coordinator-continuity protection across grants, account roles and deactivation. Learner invitation, Enrollment/roster, progress, cohort-start email, withdrawal and transfer remain later work. See the [current HTTP contract](../04-api-design/cohort-scheduling.md); this status does not reduce the MVP's remaining requirements or resolve Q69.

Schedule/staff cohorts, assign Learners, inspect roster/progress and retain run history. Public browsing/self-enrollment, cohort-less learning, per-module scheduling and implicit cohort transfer are outside MVP.

## Core flows

1. An authorized actor selects a published course, dates and eligible Coordinator. Persist cohort and initial staff assignment atomically; dates are UTC instants with local presentation. Upcoming/active/ended is derived from the schedule.
2. Assign eligible Coordinator/Facilitator grants. The same person may hold both distinct account roles and both cohort capabilities.
3. Select an active account with Learner entitlement and assign it to this cohort. Create a fresh Enrollment ID after checking organization/course consistency and the one-active-run-per-user/course rule. Record actor and audit.
4. First learning activity advances `enrolled` to `in_progress`; successful evaluation alone moves it to `completed`. Withdrawal to `dropped` follows Q69. Cohort ending is not automatic course completion.
5. Genuine re-enrollment after a terminal run creates a new ID with fresh progress, allowance and completion lifecycle. Keep every prior run and its evidence.

## Business rules

No additional learner seat is consumed by enrollment. Enrollment requires active account/Learner entitlement, current actor authority and permitted cohort access. Two concurrent assignments cannot create two active runs of the same course for one account. Retry of one assignment returns the same run, not an unintended re-enrollment.

A cohort must have an eligible Coordinator when active. Staff/account-role removals must preserve that invariant or explicitly resolve affected staffing; they must not leave an effective grant on an ineligible account. Course Author or Facilitator alone cannot assign learners. Removing an entitlement or deactivating an account blocks access without deleting/changing the identity of its run.

Cohort ID is not an editable field on Enrollment. If transfer becomes required, Q79 must specify continue-same-run versus new-run/carry-forward semantics as an explicit operation. Never derive that policy from global user/lesson keys.

## Permissions

Use [resource-grant and enrollment authority](../01-domain/roles-and-permissions.md); account-role assignment remains a different operation. The browser's visible roster is not proof of permission to mutate it.

## Data requirements

[Cohort](../03-data-model/cohort.md), [Cohort Staff](../03-data-model/cohort-staff.md), [Enrollment](../03-data-model/enrollment.md), [User](../03-data-model/user.md), [Audit](../03-data-model/audit-log.md), [Notification](../03-data-model/notification.md) for durable cohort-start email.

## Acceptance and failure cases

Reject another organization's course/user/grant IDs, a staff-only/inactive learner and an unstaffed active cohort. Concurrent assignments preserve one active run. Re-enrollment never inherits passing evidence. Removing Learner blocks future learning mutations even with an old session; staffing restrictions cannot be bypassed through account-role revocation. Clock-boundary tests cover cohort activation and idempotent start-email scheduling.

## Open questions

[Q69](../00-product/open-questions.md#q69): boundary access, withdrawal and archive. [Q79](../00-product/open-questions.md#q79): transfer only if confirmed as required.

## Related documents

[Provisioning](user-and-seat-provisioning.md) · [Core concepts](../01-domain/core-concepts.md) · [Navigation](../05-frontend/navigation.md)
