# Variable LMS — MVP scope

> **Status:** Confirmed scope baseline; feature-local gates are linked below\
> **Authority:** Canonical product scope\
> **Updated:** 2026-09-17

## Purpose

Variable LMS initially serves s2c's internal training as a customer-controlled installation. The MVP delivers assigned cohort learning with text content, quizzes, discussion and course certificates. It is a reusable product, not a vendor billing system or a multi-tenant SaaS launch.

## Included

| Capability | Required behavior and canonical specification |
|---|---|
| Organization and identity | Controlled organization/Administrator bootstrap, invite-only onboarding, profile/credential recovery, six account roles held as a set, current resource grants and revocable sessions. [Provisioning](../02-features/user-and-seat-provisioning.md), [role policy](../01-domain/roles-and-permissions.md). |
| Licensing | Locally verified signed/versioned license, max_active_learners, one seat per active Learner-entitled account, atomic capacity checks for every consuming mutation. Staff-only and pending accounts consume zero. [Contract](../04-api-design/license-contract.md). |
| Authoring | Single course owner; draft/published/archived lifecycle, ordered modules/lessons, direct publish without approval. [Authoring](../02-features/course-and-module-administration.md). |
| Learning content | Sanitized rich-text notes, required-reading links, required flag; no sequential unlocking. [Lessons](../02-features/rich-text-and-reading-lessons.md). |
| Cohorts and enrollment | Published course delivery with start/end, distinct Coordinator/Facilitator capabilities, authorized assignment, fresh Enrollment for re-enrollment and retained history. [Cohort management](../02-features/cohort-and-enrollment-management.md). |
| Assessments | Practice and graded quizzes; single-choice, multi-select, true/false; automatic scoring, run-scoped allowance, immutable submitted evidence, audited failed-allowance reset. [Quizzes](../02-features/quiz-engine.md). |
| Completion and credentials | Per-run required-lesson/graded-pass completion, visible progress, automatic fixed-template PDF certificate once per user/course with snapshotted branding. Intended Administrator void/reissue is gated by Q75. [Completion](../02-features/completion-and-certificate-generator.md). |
| Discussion | Cohort-scoped course/module Markdown forums, replies and staff moderation. [Forums](../02-features/course-and-module-forums.md). |
| Operational communication | Durable emails for invitation, cohort start, forum reply and completion, plus necessary identity recovery. SMTP is configured by the operator; no in-app notification center. |
| Delivery and integrity | OCI application release, versioned Compose package, explicit migrations, full backup/restore guidance, restricted runtime credentials, structured logs/health and mandatory sensitive-action audit. [Operations](../06-architecture/release-and-operations.md). |

## Excluded or deferred

Public catalog/self-enrollment/self-registration approval, cohort-less learning, lesson prerequisites/drip schedules, multiple course owners/collaborative editing, general file/video hosting, SCORM/xAPI/LRS, paid courses/payments, generic billing/entitlement rules, CSV provisioning/directory sync, Auditor role, global search/notification center, public certificate verification, arbitrary certificate templates, and microservices infrastructure.

Helm is the intended packaging for a supported Kubernetes path; the first release need not support Kubernetes. Future hosted tenancy, provisioning, billing and isolation require their own decisions. Transfer is only considered if Q79 establishes a need. None of these deferred items has a committed phase or date.

## Acceptance outcomes

1. An operator deploys a documented versioned package, migrates with the dedicated identity and bootstraps a usable staff-only Administrator. Customer secrets and data persist outside disposable containers.
2. An Administrator/Manager provisions authorized accounts without privilege escalation; Learner activation and grants never overrun capacity, including races. Deactivation ends access while retaining evidence.
3. An Author publishes a course, authorized staff schedule/staff a cohort and enroll a Learner. That learner reads content, passes required graded assessments and completes the correct run.
4. Completion issues one automatic course credential, repeat requests are safe, and re-enrollment has fresh learning state without duplicate automatic certificates. Process/email/PDF failures do not lose completed learning.
5. Cohort participants discuss content and authorized staff review progress/moderate within their scope. Cross-organization/cohort access is rejected.
6. Release and recovery exercises restore database, files, license/configuration and required protected keys together. Supported upgrades and rollback limits are documented and tested.

These are acceptance targets, not claims that software already exists or that setup/performance is proven. No arbitrary ten-minute setup or concurrent-user guarantee is retained.

## Open questions

Resolve [feature/release gates](../00-product/open-questions.md) before their affected work or release. In particular: reading signal, attempt behavior, multi-select, feedback, live edits, boundary access, reissue, license lifecycle and retention. Foundation work is ready; full MVP completion still requires those decisions and implementation evidence.

## Related documents

[Implementation plan](implementation-plan.md) · [Readiness](readiness.md) · [Vision](../00-product/vision.md)
