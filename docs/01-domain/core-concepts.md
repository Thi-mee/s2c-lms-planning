# Core domain baseline

> **Status:** Confirmed baseline; explicitly linked feature gates remain\
> **Authority:** Canonical — domain semantics\
> **Updated:** 2026-09-17

## Purpose

Define the minimum domain model that faithfully represents accepted behavior. September changes replace global user/course learning records with Enrollment-owned history; they do not add per-run certificates.

## Organization and account lifecycle

One Organization owns an initial self-hosted installation. Users, courses, cohorts, runs, discussion, credentials and operational records carry its identity. A User belongs to exactly one organization. All relationships must preserve organization ownership and relevant parent/course/run scope. Hosting multiple customer organizations is a separate future operating-model decision.

Account lifecycle: `pending → active → deactivated → active`. Pending invitations have no usable learner access. Deactivation disables authentication and preserves enrollments, progress, attempts, certificates and authored discussion. Reactivation requires normal authorization and any applicable learner capacity. Soft deletion is a separate administrative marker that also disables the account; actual erasure is gated by [Q57](../00-product/open-questions.md#q57).

A user holds a set of roles. The Learner role is the MVP Learner entitlement. Removing the last role may leave an authenticated, active account with only its own account-management access; it does not acquire an implicit role. Resource access still requires the relevant role and grant. See [role authority](roles-and-permissions.md).

## Learner capacity

For one organization:

`active_learners = count of distinct users with status = active, deleted_at absent, and Learner in roles`

| Account state | Learner entitlement | Additional staff roles | Consumption |
|---|---|---|---|
| pending | either | any | 0 |
| deactivated or deleted | either | any | 0 |
| active | absent | any | 0 |
| active | present | none or any | 1 |

Capacity-changing operations include activation, reactivation, adding Learner to an active account, and restoring a deleted active Learner account. Each must check the resulting count against the verified `max_active_learners` atomically. Removing Learner/deactivation releases capacity. Enrolling in more than one course does not consume additional seats. License replacement cannot race a consuming mutation. Other future entitlements may exist in the versioned protocol, but no generic billing/rules engine is implied.

## Roles and resource scope

[Roles and permissions](roles-and-permissions.md) is the single matrix. Account roles establish eligibility; `course_authors`, `cohort_staff` and `enrollments` establish context. Privileged delegation is explicit, auditable and independent from operational resource grants. Final usable Administrator continuity is protected.

## Course and content lifecycle

A Course is reusable content: `draft → published → archived`. Only published courses can launch cohorts. A Course has exactly one owner in `course_authors` for MVP. Authors publish directly; Organization Managers author only if separately holding the appropriate Course Author role/grant.

Courses contain ordered Modules; Modules contain ordered Lessons. A Lesson carries rich text, required-reading links and a `required` boolean, and may have one Quiz. Linear order does not imply unlocking prerequisites. Learners access assigned course content; there is no public catalog or self-enrollment.

Editing lesson text preserves recorded lesson completion. Reordering is visible to current learners. Changes to required flags affect **incomplete runs**, while completed evidence and issued certificates remain snapshots. Exact mutation/delete/archival behavior affecting active runs or in-flight assessments is gated by [Q69](../00-product/open-questions.md#q69) and [Q74](../00-product/open-questions.md#q74). No automatic cascade deletes historical learning evidence.

## Cohort lifecycle

A Cohort delivers one Course during a start/end window. Lifecycle: `upcoming → active → ended`, derived from the schedule for the baseline; store timestamps consistently in UTC and render clearly in the UI. At least one Coordinator assignment is required before activation. Coordinators manage logistics/membership; Facilitators support learning. Both may be assigned to one person. Per-module drip scheduling is deferred.

All content is available in the active window subject to enrollment/role checks; dates are not lesson prerequisites. Access before/after the window, dropping a learner and archiving a course with active cohorts require [Q69](../00-product/open-questions.md#q69). Do not invent a late-work or read-only policy while implementing an earlier slice.

## Enrollment and Learning Run

An **Enrollment is the Learning Run**, identified by its own immutable ID. It links one User, Cohort and Course in the same Organization. The Course must match the Cohort. At most one active run (`enrolled` or `in_progress`) exists per organization/user/course.

```text
enrolled → in_progress → completed
    └──────────┴───────→ dropped
```

Staff assignment creates an enrolled run; the first recorded learning activity advances it to in_progress. Completion evaluation alone marks completed. Dropped/completed rows remain historical; a genuine re-enrollment creates a new ID and fresh learning state, even for the same user/course. Never reactivate a historical row to simulate re-enrollment. Account deactivation does not delete or rewrite a run's historical status.

`Progress(enrollment_id, lesson_id)` and `QuizSubmission(enrollment_id, quiz_id, ...)` are the raw learning records. Optional user/course summaries derive from these records. There is no required separate Course Achievement entity.

Cohort transfer is not implemented through a generic enrollment update. If it is needed in MVP, [Q79](../00-product/open-questions.md#q79) must define an explicit continue-run/new-run operation and history/access treatment. Until then, cohort identity is not mutable through general CRUD. Re-enrollment is already defined and does not carry prior evidence implicitly.

## Assessments and completion

Practice quizzes permit unlimited submissions and do not count toward completion. Graded quizzes have a passing threshold and limited allowance for each enrollment/quiz. A submitted attempt and its per-question selections, content and scoring evidence are immutable. Authorized attempt reset creates new allowance/history; it never deletes past attempts or changes old scores.

For a particular enrollment, completion requires **all required lessons completed and at least one passing submission for every graded quiz**. Practice quizzes never count. Percentage is:

`(completed required lessons + passed graded quizzes) / (required lessons + graded quizzes) × 100`

Each lesson/quiz counts once, regardless of attempts. Evidence must belong to this enrollment and this course. Validate against a consistent set of requirements and store completion time plus requirement/evidence snapshot on the run. A zero-denominator course must not produce an accidental divide-by-zero or automatic credential; publish validation must reject a course with no completion-bearing content.

The accepted formula is not an open question. Reading-completion evidence is [Q70](../00-product/open-questions.md#q70); partial credit is [Q71](../00-product/open-questions.md#q71); attempt lifecycle is [Q72](../00-product/open-questions.md#q72); review/staff testing is [Q73](../00-product/open-questions.md#q73).

## Certificate relationship

Run completion and certificate issuance are distinct. Preserve **at most one automatic certificate per organization/user/course**, not per enrollment. A certificate records the source enrollment that first caused issuance. Later completed runs remain completed with their own evidence; they do not automatically mint another certificate.

Snapshot displayed learner/course/organization/template/branding data. Subsequent learning or content changes do not automatically revoke a credential. Administrative void/reissue is separately audited and gated by [Q75](../00-product/open-questions.md#q75). A verification ID supports authenticated lookup; it is not a promise of a signed PDF, public verifier or online dependency. Public verification remains deferred.

## Discussion, notifications and audit

Threads belong to one cohort and a matching course/module scope. Markdown posts may be nested; participants never gain another cohort's visibility merely by taking the same course. Historical threads do not migrate with a learner.

Notifications are durable transactional-email intents (invite, cohort start, forum reply, completion, plus authentication recovery email where required by identity flows). Delivery failure does not undo learning state; failure to persist required intent does not permit falsely acknowledging durable work. There is no in-app center in MVP.

Audit belongs to the business operation: role/user/license changes, membership/staff changes, publication, attempt reset, credential actions and moderation. Required audit commits with state. Retention/erasure is distinct from ordinary deactivation and remains [Q57](../00-product/open-questions.md#q57).

## Open questions

The linked questions above gate local behavior only; the [central register](../00-product/open-questions.md) remains authoritative. Organization boundary, role delegation, seats and learning-run identity are resolved.

## Related documents

[Glossary](glossary.md) · [Roles](roles-and-permissions.md) · [Entities](../03-data-model/README.md) · [Module boundaries](../06-architecture/module-boundaries.md)
