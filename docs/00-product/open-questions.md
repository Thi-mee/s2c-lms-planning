# Decision questions and implementation gates

> **Status:** Current register\
> **Authority:** Canonical for unresolved product decisions; proposals do not override accepted decisions\
> **Updated:** 2026-09-17

## Purpose

Track only decisions that remain open. Links from feature/entity documents refer here instead of inventing local answers. The [accepted decisions](accepted-decisions.md), [ADRs](../06-architecture/decisions.md) and canonical specifications have resolved the foundational architecture, authorization, seat accounting and learning-record identity questions. **No true foundational implementation blocker remains.** Feature gates below must be resolved before implementing their affected semantics; release gates must be closed before real use or supported distribution.

Historical recommendations, including ones already contradicted by later specifications, are preserved in the [pre-baseline register](../08-research/history/pre-baseline-questions.md). They are not current requirements. IDs remain stable; gaps are intentional.

## Open feature and release decisions

<a id="q68"></a>
### Q68 — License lifecycle restrictions

**Classification/gate:** Feature-local; before expired/suspended/downsize handling.

**Decision needed:** What happens to existing learner access and staff operations after expiry, a signed suspension or replacement below current usage? Is any grace period allowed?

**Already settled:** The seat predicate and atomic guard are settled. Do not deactivate accounts, delete records, choose grace periods or invent live vendor revocation. Invalid signatures never increase capacity. New consuming operations require an applicable verified entitlement.

**Affected source:** [License contract](../04-api-design/license-contract.md).

<a id="q69"></a>
### Q69 — Cohort boundaries, withdrawal and course archive

**Classification/gate:** Feature-local; before boundary/withdrawal/archive flows.

**Decision needed:** What may learners read, submit or discuss before start, after end, after withdrawal and after course archive? Who may withdraw, and when may a dropped run be replaced?

**Already settled:** All content is available within the active window without sequential locks. Cohort end does not complete a run. Coordinator/Administrator/Organization Manager authority is settled; access policy at these boundaries is not. Preserve history in every case.

**Affected source:** [Cohort management](../02-features/cohort-and-enrollment-management.md).

<a id="q70"></a>
### Q70 — Reading completion signal

**Classification/gate:** Feature-local; before learner progress UI/API.

**Decision needed:** Does completion require a Mark complete action, opening/reading evidence, or another signal? Does passing/submitting a lesson quiz also complete its reading requirement?

**Already settled:** Required lessons and graded quizzes are separate completion terms. The run-scoped identity and completion formula are settled; do not infer a reading event from a GET request or a quiz without the decision.

**Affected source:** [Reading lessons](../02-features/rich-text-and-reading-lessons.md).

<a id="q71"></a>
### Q71 — Multi-select scoring

**Classification/gate:** Feature-local; before multi-select grading.

**Decision needed:** Is scoring all-or-nothing or partial credit; if partial, how are omissions and incorrect selections treated?

**Already settled:** Single-choice/true-false scoring and immutable answer snapshots can be implemented independently. No algorithm is silently selected here.

**Affected source:** [Quiz engine](../02-features/quiz-engine.md).

<a id="q72"></a>
### Q72 — Attempt consumption, interruption and timing

**Classification/gate:** Feature-local; before real graded-attempt lifecycle.

**Decision needed:** Does an attempt consume allowance at start or submission? Can interrupted work resume, and are timers/autosave required? What ends an abandoned attempt?

**Already settled:** Server-side concurrency enforcement, request deduplication, per-run allowance and immutable submitted attempts are mandatory. Do not present draft submissions as already scored or accidentally consume twice on retry.

**Affected source:** [Quiz Submission](../03-data-model/quiz-submission.md).

<a id="q73"></a>
### Q73 — Feedback release and staff preview

**Classification/gate:** Feature-local; before result-review and preview behavior.

**Decision needed:** When can Learners see correct answers/detailed feedback? Which staff roles may test/preview quizzes, including the previously conflicting Coordinator permission?

**Already settled:** Taking-quiz responses never expose answer keys. Authorized cohort result review remains scoped by the role policy. Preview must not create learner evidence, consume real allowance or issue a certificate.

**Affected source:** [Roles](../01-domain/roles-and-permissions.md).

<a id="q74"></a>
### Q74 — Live definition changes

**Classification/gate:** Feature-local; before live quiz editing/deletion.

**Decision needed:** Which published content/quiz changes are allowed with active runs or attempts? Which definition governs an in-flight attempt and incomplete run after questions, thresholds or attempt limits change?

**Already settled:** Text edits preserve recorded lesson completion; required-flag edits recalculate incomplete runs; completed snapshots/certificates stand. Submitted answers/score snapshots cannot be rewritten. Resolve remaining policy rather than introducing a full versioned-course product speculatively.

**Affected source:** [Authoring](../02-features/course-and-module-administration.md).

<a id="q75"></a>
### Q75 — Administrative certificate void/reissue

**Classification/gate:** Feature-local; before the administrative feature.

**Decision needed:** Does reissue retain credential identity or replace it, and how is the old/void credential represented and discoverable? Which reasons and display updates are permitted?

**Already settled:** Administrator-only audited operation is intended. Automatic issuance remains once per user/course, linked to its source run. Public verification is deferred. Do not change automatic issuance to once per enrollment to work around reissue.

**Affected source:** [Certificate](../03-data-model/certificate.md).

<a id="q57"></a>
### Q57 — Retention, export and erasure policy

**Classification/gate:** Feature-local and release gate; before real personal data.

**Decision needed:** Define customer retention/erasure/export expectations for accounts, submissions, forums, certificates, audit, delivery records and backups; resolve certificate/audit evidence versus personal-data removal.

**Already settled:** Deactivation is not erasure. Preserve histories during ordinary lifecycle changes; no indefinite-retention compliance promise or destructive cascade is accepted. This register specifies required product policy, not legal advice.

**Affected source:** [Security and identity](../06-architecture/security-and-identity.md).

<a id="q61"></a>
### Q61 — Measurable operating targets

**Classification/gate:** Feature-local release qualification.

**Decision needed:** Which baseline hardware, concurrent workload, latency/availability target, acceptable upgrade downtime, backup RPO and recovery RTO does the first supported release promise?

**Already settled:** 1,000–10,000 learner accounts is a planning aspiration, not concurrent-user capacity or a performance guarantee. Engineering can measure the walking skeleton before promises are set; restoration must be tested regardless.

**Affected source:** [Release and operations](../06-architecture/release-and-operations.md).

<a id="q63"></a>
### Q63 — First supported deployment targets

**Classification/gate:** Feature-local release packaging.

**Decision needed:** Which customer environments and CPU/platform combinations must the first release support, and is Helm support required at that release or later?

**Already settled:** Versioned Compose is the first implementation path; Kubernetes is not required to run the product. Helm is the intended Kubernetes packaging mechanism when supported. No simultaneous Compose/Helm/SaaS launch commitment is accepted.

**Affected source:** [ADR-5](../06-architecture/decisions.md#adr-5-container-image-delivery-model).

<a id="q41"></a>
### Q41 — Formal accessibility target

**Classification/gate:** Feature-local release acceptance.

**Decision needed:** Which explicit accessibility conformance target and evidence are required for the first release?

**Already settled:** Semantic controls, keyboard operation, focus handling and accessible validation are engineering baseline requirements. Earlier WCAG wording was a recommendation, not a verified conformance commitment.

**Affected source:** [Product principles](product-principles.md).

<a id="q78"></a>
### Q78 — External distribution terms

**Classification/gate:** Feature-local; before external commercial distribution.

**Decision needed:** What source/binary license, customer distribution terms and third-party notices will Variable publish?

**Already settled:** No open-source license or price tier is selected by this consolidation. Signed learner capacity is an LMS contract; vendor pricing/billing remains external. Implementation may begin without choosing commercial tier names.

**Affected source:** [Vision](vision.md).

<a id="q79"></a>
### Q79 — Cohort transfer, only if required

**Classification/gate:** Feature-local conditional scope.

**Decision needed:** Is transfer actually required in MVP? If yes, when does it continue one run versus create a new run, and what explicit evidence carry-forward is allowed?

**Already settled:** No transfer API is planned until required. Genuine re-enrollment always creates a new run. Do not implement transfer by editing cohort IDs or sharing user/lesson progress. This question does not block ordinary assignment/re-enrollment.

**Affected source:** [Enrollment](../03-data-model/enrollment.md).

## Engineering decisions — team may proceed

These require implementation evidence, not another product meeting. Record a small ADR only if a choice has lasting architectural consequences.

| Work | Baseline / discretion |
|---|---|
| Persistence and boundaries | EF Core/Npgsql is the default; deliberate SQL is allowed. Choose module schemas/naming, constraints and architecture-test library under the accepted ownership rules. |
| Sessions and identity | ASP.NET Core Identity/cookies with PostgreSQL ticket storage and immediate current-state validation; choose concrete store implementation and password/rate-limit settings, test revocation. |
| API details | Same-origin HTTP/JSON and explicit commands are the baseline; choose endpoint naming, pagination and error shapes as vertical slices are built. No GraphQL decision is needed. |
| Search/navigation | One adaptive web shell, contextual filtering/search; PostgreSQL indexes/full-text where justified. No external search engine or global catalog required. |
| License interoperability | Versioned signed envelope and trusted-key validation; verify the JWS/ES256 profile and choose its library via issuer/LMS test vectors before Licensing implementation. No billing engine. |
| Durable work | PostgreSQL persisted intent with leases/retries and stable business dedupe; choose polling/batching/backoff. Add non-email work storage only for a real asynchronous operation. |
| PDF and files | Choose compatible .NET PDF tooling; test fixed-template rendering, dependencies and protected assets. Persistent local files suffice initially; object storage is optional. |
| Build/release | Select registry, CI provider and dependency pins; implement reproducible OCI/package release and upgrade/restore checks. No application code or CI configuration is created in this pass. |
| Team/program inputs | Team size, dates and estimates belong to delivery planning. No unapproved dates or fixed later phases are promised. |

## Resolved or retired question routing

This table closes stale questions without copying the full canonical answer. “Deferred” is a scope disposition, not an unanswered foundational blocker. Commercial/program inputs outside the LMS are explicitly identified.

| Earlier IDs | Disposition / canonical source |
|---|---|
| Q1, Q3, Q7 | Variable LMS for organizations; s2c first customer; self-hosted distribution and future hosting are separate from application topology. [Vision](vision.md), [D1–D4](accepted-decisions.md). |
| Q2, Q2b, Q19, Q27, Q47 | Cohort assignment; ordered content without hard locks; cohort dates only. Public catalog, self-enrollment, bookmarks and drip schedules deferred. [Core concepts](../01-domain/core-concepts.md), [MVP](../07-roadmap/mvp.md). |
| Q4 | Scale aspiration retained; actual qualification is Q61. |
| Q5, Q6, Q17, Q25, Q67 | Paid courses/subscriptions are future possibilities; no MVP billing. Vendor issuance/control plane separate; limits from signed file, no in-app seat-increase approval. Price bands are external commercial inputs. [License contract](../04-api-design/license-contract.md), ADR-12. |
| Q8, Q31, Q45 | Practice/graded quizzes, per-question evidence and permissioned audited allowance resets. [Quiz feature](../02-features/quiz-engine.md). |
| Q9, Q20, Q44, Q-completion | Automatic course credential, fixed configurable branding, exact run-scoped completion. [Completion feature](../02-features/completion-and-certificate-generator.md). Reissue is Q75. |
| Q10, Q18, Q22, Q24, Q26, Q51, Q66 | Six explicit account roles, multiple memberships, one course owner, distinct Coordinator/Facilitator grants and explicit enrollment/grant authority. [Role policy](../01-domain/roles-and-permissions.md). |
| Q11, Q12, Q29, Q32 | Text/readings and sanitized Markdown forums. General uploads/video/SCORM/xAPI deferred. [MVP](../07-roadmap/mvp.md). |
| Q13, Q14, Q15, Q38, Q64, Q65 | .NET 10/ASP.NET Core modular monolith, React/Vite, PostgreSQL, justified Redis only; boundaries and tests. [ADRs](../06-architecture/decisions.md). |
| Q16 | Prebuilt VM images deferred; current release contract is OCI images plus versioned deployment packages. |
| Q21, Q30, Q33, Q46, Q49 | Invite-only, pending state, explicit Learner seat predicate and preserved records on deactivation. [Provisioning](../02-features/user-and-seat-provisioning.md). |
| Q23 | Auditor role deferred; do not promise that adding a role later is only a configuration change. |
| Q28, Q42 | Privacy principle accepted; actual score/data retention consolidated into Q57. |
| Q34, Q35, Q37, Q56 | Engineering defaults recorded in [navigation](../05-frontend/navigation.md) and API guidance; no unresolved product architecture gate. |
| Q36, Q55 | Durable transactional email; in-app notification center deferred. [Notification](../03-data-model/notification.md). |
| Q39, Q59, Q60, Q62 | Invariant-driven tests, structured observability, rate limits and mandatory transactional audit. [Invariants](../06-architecture/implementation-invariants.md), [operations](../06-architecture/release-and-operations.md). Concrete tooling remains engineering. |
| Q40, Q54, Q58 | Explicit deployment migration, compatibility, full backup and restore. [Operations](../06-architecture/release-and-operations.md). Numeric recovery targets are Q61. |
| Q43 | No new industry-specific compliance product promised. |
| Q48 | Owning Authors publish directly; no approval queue. [Authoring](../02-features/course-and-module-administration.md). |
| Q50 | Revocable opaque cookie sessions; Redis not mandatory. [Security](../06-architecture/security-and-identity.md), ADR-11. |
| Q53 | Organization ownership and trusted scope/constraints now; shared-database SaaS/RLS operating model is a separate future decision. ADR-9. |
| Q50r, Q51r, Q52 | Delivery estimates/team/deadlines are program inputs, not architectural blockers. [Implementation plan](../07-roadmap/implementation-plan.md). |

<!-- Stable aliases for closed questions; see routing table above. -->
<a id="q1"></a>
<a id="q2"></a>
<a id="q3"></a>
<a id="q4"></a>
<a id="q5"></a>
<a id="q6"></a>
<a id="q7"></a>
<a id="q8"></a>
<a id="q9"></a>
<a id="q10"></a>
<a id="q11"></a>
<a id="q12"></a>
<a id="q13"></a>
<a id="q14"></a>
<a id="q15"></a>
<a id="q16"></a>
<a id="q17"></a>
<a id="q18"></a>
<a id="q19"></a>
<a id="q20"></a>
<a id="q21"></a>
<a id="q22"></a>
<a id="q23"></a>
<a id="q24"></a>
<a id="q25"></a>
<a id="q26"></a>
<a id="q27"></a>
<a id="q28"></a>
<a id="q29"></a>
<a id="q30"></a>
<a id="q31"></a>
<a id="q32"></a>
<a id="q33"></a>
<a id="q34"></a>
<a id="q35"></a>
<a id="q36"></a>
<a id="q37"></a>
<a id="q38"></a>
<a id="q39"></a>
<a id="q40"></a>
<a id="q42"></a>
<a id="q43"></a>
<a id="q44"></a>
<a id="q45"></a>
<a id="q46"></a>
<a id="q47"></a>
<a id="q48"></a>
<a id="q49"></a>
<a id="q50"></a>
<a id="q51"></a>
<a id="q52"></a>
<a id="q53"></a>
<a id="q54"></a>
<a id="q55"></a>
<a id="q56"></a>
<a id="q58"></a>
<a id="q59"></a>
<a id="q60"></a>
<a id="q62"></a>
<a id="q64"></a>
<a id="q65"></a>
<a id="q66"></a>
<a id="q67"></a>
<a id="q2b"></a>
<a id="q50r"></a>
<a id="q51r"></a>
<a id="q-completion"></a>

## Updating this register

Record the decision and date here, update the canonical feature/domain/ADR, and remove conflicting active wording. Keep historical reasoning in research rather than parallel current answers. Use [source precedence](../README.md) if documents disagree. Implementation must not quietly choose substantial product behavior at an open gate.

## Related documents

[Readiness assessment](../07-roadmap/readiness.md) · [Implementation sequence](../07-roadmap/implementation-plan.md) · [Accepted decisions](accepted-decisions.md)
