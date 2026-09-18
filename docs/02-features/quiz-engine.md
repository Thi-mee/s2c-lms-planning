# Feature: Quiz engine

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Assessment\
> **Updated:** 2026-09-17

## Purpose and users

Authors create practice and graded quizzes. Enrolled Learners answer them; authorized staff review results and can reset an exhausted failed graded allowance.

## Goal and non-goals

Support single-choice, multi-select and true/false questions, automatic scoring, stored per-question answers, practice retries and capped graded attempts. Essay/manual grading, question banks and random pools are outside MVP. Timing/autosave and feedback release are unresolved, not promised features.

## Core flows

1. Author configures the lesson's quiz: type, questions/options/points and, for graded quizzes, passing percentage and positive attempt limit.
2. Learner starts/submits against a specified enrollment. Validate current identity, run/course context, definition and allowance. Claim attempts according to Q72; never enforce only in the browser.
3. Persist submission, answer/definition snapshots, calculated score and pass result together. A submitted attempt is immutable. Deduplicate retransmission using a request key scoped to the authenticated account/run.
4. Feed a graded pass into completion evaluation in the same local transaction, or persist durable completion work before acknowledging success. Practice outcomes do not affect course completion.
5. Authorized staff reset the exhausted failed graded allowance with a reason. Start a new allowance epoch for that run/quiz, retaining all earlier submissions and reset audit history.

## Business rules

Practice attempts are unlimited. Graded attempts are bounded per **learning run and quiz**, not global user/quiz. A new genuine enrollment starts with fresh allowance and no passing evidence. A reset grants a fresh configured allowance in a new epoch; it does not delete attempts, renumber old submissions, change old scores or create a new learning run.

`passed = earned_points / available_points * 100 >= passing_score_percentage`; require positive available points and use exact calculation for the boundary, not rounded display percentages. Single-choice and true/false award configured points for the correct selection. Multi-select calculation is Q71. Any qualifying graded pass in this run satisfies that quiz for completion; resetting a failed allowance cannot invalidate an already issued certificate.

Never send correctness flags or scoring snapshots in a taking-quiz response. Expose feedback only under Q73. Staff preview, if enabled, must not consume learner allowance or contribute progress, completion or certificates. Answer snapshots protect historical interpretation when authors later edit definitions; in-flight definition selection still requires Q74.

## Permissions

[Canonical permissions](../01-domain/roles-and-permissions.md) govern authoring, learner attempts, cohort/course result review and resets. Facilitators/Coordinators require their specific cohort grant; Authors require ownership; Administrators remain organization-scoped. Organization Manager alone cannot reset attempts or read detailed answers.

## Data requirements

[Quiz](../03-data-model/quiz.md), [Question](../03-data-model/quiz-question.md), [Option](../03-data-model/quiz-option.md), [Submission and allowance epochs](../03-data-model/quiz-submission.md), [Submission Answer](../03-data-model/quiz-submission-answer.md), [Enrollment](../03-data-model/enrollment.md), [Audit](../03-data-model/audit-log.md).

## Acceptance and failure cases

Race two submissions for the final allowance; at most one new consumed attempt succeeds. Retry a committed submission after a lost response; return the same outcome. Race reset with submission; serialize the result and audit it. Re-enrollment cannot inherit previous passes; reset cannot rewrite past submissions. Definition/answer tampering, cross-run IDs and leaked answer keys fail negative tests. Worker restart must not lose a committed completion trigger.

## Open questions

[Q71](../00-product/open-questions.md#q71) scoring; [Q72](../00-product/open-questions.md#q72) attempt consumption/resume/timing; [Q73](../00-product/open-questions.md#q73) feedback/preview; [Q74](../00-product/open-questions.md#q74) live edits; [Q57](../00-product/open-questions.md#q57) retention.

## Related documents

[Completion](completion-and-certificate-generator.md) · [Navigation](../05-frontend/navigation.md) · [Module boundaries](../06-architecture/module-boundaries.md)
