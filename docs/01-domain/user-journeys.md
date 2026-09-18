# User journeys

> **Status:** Confirmed baseline with feature gates\
> **Authority:** Supporting — derives from core concepts and feature specifications\
> **Updated:** 2026-09-17

## Purpose

Describe the actual assigned-cohort MVP. Former public browsing/self-enrollment, sequential unlocking and publication approval journeys are superseded.

## Establish the installation

Operator applies the versioned deployment package, configures PostgreSQL/storage/SMTP/license/key material and establishes the first Administrator through controlled bootstrap. The Administrator manages organization users and explicitly grants permitted roles; the installation preserves a usable Administrator.

## Author and run a course

A Course Author creates a draft course with one ownership grant, ordered modules, lessons/required flags/readings and quizzes. They preview content, validate requirements and publish directly. A Coordinator chooses a published course from the staff library, configures a cohort window and eligible staff, and assigns learners. Facilitators support delivery, not account provisioning or cohort membership management.

## Invite, activate and assign

Manager/Admin invites an account with only permitted roles. Pending invitation consumes zero seats. Acceptance sets credentials and activates with an atomic learner-capacity check. Activation failure at capacity leaves the account pending and retryable. Staff-only activation consumes zero. Coordinator/Manager/Admin assigns an active Learner to a cohort, creating a distinct Enrollment.

## Learn and complete

Learner signs in and sees assigned cohorts, opens a run's course, reads required content and takes graded quizzes. Lessons are ordered but do not unlock sequentially. The accepted completion formula evaluates only this enrollment's evidence. The run completes; the system issues a certificate only if the user/course does not already have one, and records a durable completion email. The learner can view run history and their credential.

Reading acknowledgement, attempt timing and post-window access are feature gates; see [questions](../00-product/open-questions.md). Do not invent automatic page-view completion in the UI.

## Support and discussion

Learner posts a Markdown question in their cohort's course/module forum. Authorized staff reply and moderate; the original poster receives a reply email. Staff view only authorized run records. Authorized reset restores quiz allowance through an auditable action without deleting prior submissions.

## Deactivate or re-enroll

Manager/Admin deactivates an eligible account through authorized user management: access/session validity ends, learner capacity is released, and records remain. Reactivation rechecks learner capacity. Genuine re-enrollment creates a new run with fresh progress and attempts; previous evidence remains historical. A cohort transfer is not a generic edit and needs its own decision if introduced.

## Open questions

[Q68–Q75 and Q79](../00-product/open-questions.md) gate the relevant exceptional flows. No catalog, notification-center or approval workflow is included in MVP.

## Related documents

[Core concepts](core-concepts.md) · [Role policy](roles-and-permissions.md) · [Features](../02-features/README.md) · [Navigation](../05-frontend/navigation.md)
