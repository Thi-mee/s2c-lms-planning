# Feature: Completion and certificate generation

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Progress & Certification; Enrollment & Cohorts\
> **Updated:** 2026-09-17

## Purpose and users

Learners and authorized staff need deterministic completion for each learning run and a stable, downloadable course credential.

## Goal and non-goals

Automatically evaluate run completion and issue a PDF using one fixed template with organization name, logo and signature line. Arbitrary templates, public verification, expiring credentials and per-run automatic certificates are outside the accepted MVP.

## Core flows

1. Read current required lessons and graded quizzes plus evidence belonging only to the enrollment being evaluated.
2. Mark that enrollment completed when every required lesson is complete and every graded quiz has a passing submission. Persist requirement/evidence snapshot and completion time through Enrollment's public interface.
3. If the organization/user/course has no automatically issued certificate, issue it with immutable display data and a link to this source enrollment. If another run already produced it, retain the existing credential; the new run still records its own completion.
4. Persist a completion email intent with the completion transaction. Render/download the protected PDF from the certificate snapshot; retrying render or email must not reissue the credential.

## Business rules

Run percentage is `(completed required lessons + passed graded quizzes) / (total required lessons + total graded quizzes) * 100`, counting each lesson/quiz once. Practice never counts. Reject zero-denominator publication as the engineering validation default. Percent display rounding is not the completion predicate.

A genuine re-enrollment has fresh progress and attempts, while certificates remain **one automatic certificate per organization/user/course**. Learning history and the credential are separate concepts. Completion evaluation is idempotent under concurrent final lesson/quiz events. Use database uniqueness and one local transaction for completed state, snapshot, any new certificate, required audit and notification intent.

Text edits do not revoke prior progress. Changed required flags recompute incomplete runs; completed evidence snapshots and certificates stand. Preserve certificate display values and branding assets even if current names/logo change. Administrative void/reissue is a separate product operation gated by Q75, not an excuse to loosen the automatic-issuance uniqueness rule.

`verification_id` is a unique, opaque credential reference. It is not a cryptographic proof, not a hash of mutable personal data, and does not imply an anonymous verification endpoint. MVP lookup/download requires authorization. Privacy/erasure policy must determine what snapshots remain; do not promise perpetual verification after all personal records have been erased.

## Permissions

[Canonical permissions](../01-domain/roles-and-permissions.md) control progress/result visibility, own certificate access, branding and privileged administrative certificate actions. Staff earn credentials only through Learner entitlement and an actual completed run.

## Data requirements

[Enrollment](../03-data-model/enrollment.md), [Progress](../03-data-model/progress.md), [Quiz Submission](../03-data-model/quiz-submission.md), [Certificate](../03-data-model/certificate.md), [Notification](../03-data-model/notification.md), [Audit](../03-data-model/audit-log.md).

## Acceptance and failure cases

Repeated/concurrent evaluation issues one credential and one completion intent per run. A second completed run does not issue another automatic certificate. Failure to persist audit/work rolls back the completion transition; SMTP failure does not. A failed PDF render can retry without changing credential identity. Cross-organization/run evidence cannot satisfy completion; answer histories and completed snapshots survive author edits and account deactivation.

## Open questions

[Q75](../00-product/open-questions.md#q75): administrative void/reissue. [Q57](../00-product/open-questions.md#q57): snapshot retention. PDF library choice is engineering work with rendering, font and dependency-license validation.

## Related documents

[Core concepts](../01-domain/core-concepts.md) · [Quiz engine](quiz-engine.md) · [Release/storage guidance](../06-architecture/release-and-operations.md) · [Navigation](../05-frontend/navigation.md)
