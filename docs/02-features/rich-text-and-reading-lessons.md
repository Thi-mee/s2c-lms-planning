# Feature: Rich text and reading lessons

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Course Authoring; Progress & Certification\
> **Updated:** 2026-09-17

## Purpose and users

Authors prepare lesson notes and external required-reading links. Learners consume them within their assigned learning run and record completion.

## Goal and non-goals

MVP supports sanitized rich text, lesson notes, titled external reading URLs, lesson ordering and a required flag. Native video/file uploads, SCORM/xAPI, sequential unlocking and per-module drip scheduling are excluded.

## Core flows

1. Authorized author creates/edits a lesson inside an owned course module, with notes, reading links and `required` flag.
2. Learner selects an assigned cohort and learning run, then opens any available lesson in order or directly. Order is guidance; previous lesson completion is not a gate.
3. An explicit completion signal, determined by Q70, writes progress for that **enrollment and lesson**. Repeat requests are idempotent and trigger completion evaluation.

## Business rules

Access requires active account, Learner entitlement, the correct run/course and cohort access policy. Staff reading/preview is not learning progress. Rich text and external URLs are sanitized/validated; external reading availability is outside the LMS and does not imply the entire course works without internet access.

A completed record retains the content version/basis used. Editing text does not erase completion. Required-flag changes recalculate incomplete runs only; completed run snapshots and issued certificates stand. Required lessons and graded quizzes are counted separately in the [completion rule](completion-and-certificate-generator.md).

## Permissions

Use [canonical resource permissions](../01-domain/roles-and-permissions.md). Learning writes are for one's own active run; staff may not fabricate a learner's read progress through their support role.

## Data requirements

[Lesson](../03-data-model/lesson.md), [Module](../03-data-model/module.md), [Enrollment](../03-data-model/enrollment.md), [Progress](../03-data-model/progress.md).

## Acceptance and failure cases

Opening Lesson 2 before Lesson 1 succeeds during the allowed cohort window. Two runs by the same learner have independent progress. Duplicate completion signals create one progress record. Reject cross-course lesson IDs, staff previews writing progress, unauthorized runs and unsafe rendered HTML/URLs.

## Open questions

[Q70](../00-product/open-questions.md#q70): what proves reading complete, including whether a quiz submission also marks its lesson complete. [Q69](../00-product/open-questions.md#q69): window boundaries. [Q74](../00-product/open-questions.md#q74): other live edits.

## Related documents

[Completion](completion-and-certificate-generator.md) · [Navigation](../05-frontend/navigation.md) · [Authoring](course-and-module-administration.md)
