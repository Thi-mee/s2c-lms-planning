# Open Questions — Historical snapshot

> **Authority:** Historical — superseded on 2026-09-17.
> **Do not implement from this snapshot.** Preserved from the pre-consolidation working tree, including uncommitted proposals. Use the [current source map](../../README.md) and [accepted decisions](../../00-product/accepted-decisions.md). Original status labels below describe the old state only.

> **Status:** Living Document\
> **Last Updated:** July 2026

---

## Purpose

This document tracks unresolved questions that affect product direction, feature design, or architecture. Questions should be moved to "Resolved" once answered, with a brief rationale.

This is the **single source of truth** for open product questions. Do not scatter open questions across documents without also listing them here. Local documents may repeat a question for context, but each must reference its canonical `Q#` here.

Each open question below includes **Pros**, **Cons**, and a **Recommendation** (the author's expert opinion). Recommendations are advisory — they are proposals to accelerate decision-making, not decisions. Once a decision is made, move the row to **Resolved** and update the affected documents.

---

## How This Document Is Organized

Open questions are grouped by theme:

- [1. Product Direction & Monetization](#1-product-direction--monetization)
- [2. Users & Roles](#2-users--roles)
- [3. Learning Model & Domain Rules](#3-learning-model--domain-rules)
- [4. Data Model](#4-data-model)
- [5. Frontend & UX](#5-frontend--ux)
- [6. Architecture & Engineering](#6-architecture--engineering)
- [7. Cross-Cutting Concerns Not Yet Addressed](#7-cross-cutting-concerns-not-yet-addressed)
- [8. Product Principles](#8-product-principles)
- [9. Delivery & Roadmap](#9-delivery--roadmap)

Legacy questions Q1–Q15 remain in the [Resolved](#resolved) table for historical reference.

---

## 1. Product Direction & Monetization

### Q7 — Is this a hosted SaaS or self-hosted product?

**Source:** [vision.md](../../00-product/vision.md) · **Impact:** Very high (architecture, billing, support model)

- **Pros (of committing to self-hosted-first, SaaS-capable):** Matches ADR-1/ADR-2 (single binary, env-based config); maximizes the "data sovereignty" differentiator; smallest ops burden for s2c early on.
- **Cons:** Leaving both fully open indefinitely means multi-tenancy isolation (see [Q53](#q53--multi-tenancy-isolation-strategy)) stays undecided, and that is expensive to retrofit; support/upgrade tooling differs sharply between the two models.

**Recommendation:** Commit explicitly to **self-hosted single-tenant as the primary product**, with SaaS multi-tenant as a *later, deliberately-designed* mode — not an ambient "maybe." Then resolve [Q53](#q53--multi-tenancy-isolation-strategy) now, because the tenancy decision constrains the schema even in single-tenant mode. Keeping it vague is the single biggest latent risk in the plan.

> **Update (July 2026):** The delivery model is now specified as **vendor-published, versioned container images + a Docker Compose (baseline) or Helm/K8s (scale) manifest** applied in customer-owned infrastructure, with managed SaaS running the same images — see [ADR-5](pre-baseline-adrs.md#adr-5-container-image-delivery-model) and [architecture-evaluation.md](../architecture-evaluation.md). ⚠️ **Doc drift:** [vision.md](../../00-product/vision.md) and [goals.md](../../00-product/goals.md) still describe a "single binary"; that wording is superseded by ADR-5 (which supersedes [ADR-1](pre-baseline-adrs.md#adr-1-technical-stack-selection--pragmatic-react-spa)) and must be updated once ADR-5 is accepted.

### Q16 — Which VM image format should we prioritize first?

**Source:** [vision.md](../../00-product/vision.md) · **Impact:** Low (deployment convenience)

- **Pros (of prioritizing one, e.g., AWS AMI):** A prebuilt image lowers the self-hosting bar dramatically; AWS has the largest enterprise footprint.
- **Cons:** Image maintenance is per-cloud toil; premature if the binary + Postgres story isn't proven yet.

**Recommendation:** **Defer.** Ship a single static binary + a `docker-compose.yml` and a documented systemd unit first — these cover ~90% of self-hosters with near-zero maintenance. Add a prebuilt **AWS AMI** only once there is real customer demand. Not an MVP blocker.

### Q17 — What are the seat-tier thresholds for the yearly license?

**Source:** [vision.md](../../00-product/vision.md) · **Impact:** Medium (billing, licensing gatekeeper)

- **Pros (of fixed tiers, e.g., 500 / 1,000 / 5,000):** Simple to communicate and to encode in the signed `license.lic` file; predictable revenue bands.
- **Cons:** Fixed tiers create awkward cliffs (a 1,050-seat customer buys the 5,000 tier); a continuous "max_seats" integer is more flexible.

**Recommendation:** Technically, encode a **single `max_active_seats` integer** in the license file (see [ADR-3](pre-baseline-adrs.md)) — the gatekeeper only cares about the number. Treat the *published tiers* as a **sales/pricing artifact**, not an engineering constraint. This decouples billing packaging from code and lets sales change bands without a release. Exact price points are a business decision, not a development blocker.

### Q20 / Q44 — Certificate customization and MVP template format

**Source:** [goals.md](../../00-product/goals.md), [mvp.md](../../07-roadmap/mvp.md) · **Impact:** Medium

- **Pros (of full custom HTML/CSS templates):** Strong white-label story; organizations want their own branding.
- **Cons:** Arbitrary HTML/CSS → PDF is a security and rendering-fidelity rabbit hole (sandboxing, fonts, page breaks); large scope for MVP.

**Recommendation:** For **MVP**, ship **one fixed, well-designed certificate template** with configurable fields only: organization name, logo image, and signature line. Defer arbitrary HTML/CSS templating to a later phase. This resolves Q44 (fixed template is sufficient) and scopes Q20 (customization = a later feature). See [completion-and-certificate-generator](../../02-features/completion-and-certificate-generator.md).

---

## 2. Users & Roles

### Q22 — Can a single account hold multiple roles? *(Consistency conflict — decide first)*

**Source:** [target-users.md](../../00-product/target-users.md) (open) vs. [roles-and-permissions.md](../../01-domain/roles-and-permissions.md) (assumes single primary role) · **Impact:** Very high (authorization model)

> ✅ **Resolved (July 2026):** An account holds a **set** of account roles (Layer 1), scoped to specific courses/cohorts by contextual grants (Layer 2: [`course_authors`](../../03-data-model/course-author.md), [`cohort_staff`](../../03-data-model/cohort-staff.md), [`enrollments`](../../03-data-model/enrollment.md)). A person can author one course, learn in another, and facilitate a third — with no duplicate accounts. [roles-and-permissions.md](../../01-domain/roles-and-permissions.md), [user.md](../../03-data-model/user.md), and [target-users.md](../../00-product/target-users.md) now reflect this two-layer model. The former conflict is closed. *(Pros/cons below retained for history.)*

- **Pros (of single primary role + implicit learner capability):** Clean authorization logic; simplest mental model; covers the common "instructor who also takes courses" case.
- **Cons:** Real orgs have people who are genuinely both Instructor *and* Coordinator, or Admin *and* Instructor; forcing one role leads to duplicate accounts.

**Recommendation:** Adopt a **primary role for authorization + a "can enroll as learner" capability for everyone** (which the docs already lean toward), and model role as a **set internally even if the UI exposes one primary role**. Storing roles as a collection from day one costs almost nothing and avoids a painful migration if true multi-role is needed later (consistent with Principle 5, "decisions should be reversible"). **Update `target-users.md` to reference this Q22 rather than posing it independently.**

### Q21 / Q46 / Q49 — Learner onboarding: self-registration, first-run experience, course assignment

**Source:** [target-users.md](../../00-product/target-users.md), [user-journeys.md](../../01-domain/user-journeys.md) · **Impact:** High (MVP flows)

- **Pros (of invite-only + Org-Manager approval for self-registration):** Tight control over seat consumption (critical given the licensing model); predictable onboarding; fits B2B/internal-s2c MVP.
- **Cons:** Pure invite-only adds admin overhead at scale; self-registration is more convenient but risks silently exhausting seats.

**Recommendation:** For **MVP**, make onboarding **invite-only** (Org Manager / Admin invites → learner accepts via tokened email → sets password). This directly reinforces the seat gatekeeper ([ADR-3](pre-baseline-adrs.md)) and sidesteps the approval-queue complexity of self-registration. Add **optional self-registration-with-approval** later behind a config flag (Q21 → later). Course assignment (Q49): the Org Manager assigns a learner to a **cohort**, and enrollment follows from cohort membership — there is no separate "assign course" primitive. First-run experience (Q46): a minimal dashboard listing assigned cohorts/courses is sufficient for MVP.

### Q23 — Read-only Auditor role?

**Source:** [target-users.md](../../00-product/target-users.md) · **Impact:** Low–Medium (compliance)

- **Pros:** Valuable for regulated buyers; read-only is low-risk to implement once authorization exists.
- **Cons:** Adds a sixth role to every permission decision and test matrix; no confirmed buyer demand yet.

**Recommendation:** **Defer past MVP**, but design the authorization layer so a read-only role is a *derived permission set* (all `read`, no `write`) rather than a special case. That way adding Auditor later is configuration, not surgery.

### Q24 — Can Organization Managers author org-only courses?

**Source:** [roles-and-permissions.md](../../01-domain/roles-and-permissions.md) · **Impact:** Medium

- **Pros (of keeping authoring with Course Authors/Admins only):** Clear separation of concerns; simpler permission matrix; matches the current matrix (Org Manager = ❌ create course).
- **Cons:** Some orgs conflate "manager" and "content owner"; a hard wall may frustrate small teams.

**Recommendation:** **Keep authoring out of the Org Manager role for MVP** (as the matrix already states). If a manager needs to author, they can *also* hold the Course Author role (see Q22's role-set model). Confirm and close.

### Q25 — Approval step for seat-limit increases?

**Source:** [roles-and-permissions.md](../../01-domain/roles-and-permissions.md) · **Impact:** Low (the license file is the real gate)

- **Pros (of no in-app approval):** The signed `license.lic` is the authoritative limit; s2c issues a new file when a customer upgrades — no in-app workflow needed.
- **Cons:** Customers can't self-service an urgent bump without contacting s2c.

**Recommendation:** **No in-app approval for MVP.** Seat increases happen out-of-band (customer pays → s2c issues a new signed license → customer drops it in). This keeps [ADR-3](pre-baseline-adrs.md) offline-verifiable. Revisit only if a SaaS mode with automated billing arrives.

### Q26 — Instructor co-owners / collaborators on a course?

**Source:** [roles-and-permissions.md](../../01-domain/roles-and-permissions.md) · **Impact:** Medium (authoring data model)

- **Pros (of single-owner for MVP):** Simplest ownership check (`course.owner_id == user.id`).
- **Cons:** Real course-building is collaborative; retrofitting many-to-many authorship later touches every authoring permission check.

**Recommendation:** **Single owner for MVP**, but model course authorship via a **join table (`course_authors`) from the start** rather than a scalar `owner_id`. Populating it with one row today makes multi-author a data change, not a schema migration (Principle 5). See [course.md](../../03-data-model/course.md).

### Q66 — Cohort Coordinator and Learning Facilitator: one role or two?

**Source:** [roles-and-permissions.md](../../01-domain/roles-and-permissions.md) · **Impact:** Medium (role model, staffing)

- **Pros (of two capabilities on one role):** For a small internal MVP the same person usually handles both logistics and support; two separate account roles doubles the permission/test surface for no real separation.
- **Cons:** Larger orgs genuinely split logistics (Coordinator) from learner support (Facilitator); collapsing them entirely loses that distinction.

**Recommendation:** Model both as **capabilities on the [`cohort_staff`](../../03-data-model/cohort-staff.md) join** (`capability ∈ {coordinator, facilitator}`), and for MVP allow one person to hold both on a cohort. This preserves the conceptual separation now and enables full role-splitting later with no migration. Confirm during implementation.

---

## 3. Learning Model & Domain Rules

### Q2b — Reconcile cohort-based vs. self-paced learner journey *(Consistency conflict)*

**Source:** [core-concepts.md](../../01-domain/core-concepts.md) (enrollment = learner→cohort) vs. [user-journeys.md](../../01-domain/user-journeys.md) ("Browse → Enroll" self-serve) · **Impact:** High

> ⚠️ The domain model ties every enrollment to a **cohort**, but the learner journey reads like open self-serve enrollment. [Q2](#resolved) resolved this as "hybrid, cohort-first," but the journeys doc wasn't updated to match.

- **Pros (of cohort-first for MVP):** Matches the Cohort Coordinator / Learning Facilitator roles, cohort-scoped forums, and the internal-s2c use case.
- **Cons:** The "browse and self-enroll" journey implies a self-paced flow that isn't actually in MVP scope.

**Recommendation:** **Cohort-first for MVP.** Learners are *assigned to a cohort*; they do not self-enroll from a public catalog in MVP. **Rewrite the "Discover and enroll" journey** in `user-journeys.md` to reflect assignment-based entry, and reference [Q2](#resolved). Self-paced (cohort-less) enrollment becomes a later mode.

### Q19 — Cohort schedules with per-module open/close dates?

**Source:** [goals.md](../../00-product/goals.md) · **Impact:** Medium

- **Pros (of drip-scheduling modules by date):** Enforces pacing; common in cohort training.
- **Cons:** Adds scheduling logic, timezone handling, and "content locked until X" UI — meaningful scope.

**Recommendation:** **MVP: cohort has a single `start_date`/`end_date` only; all content is open within that window.** Defer per-module drip scheduling to a later phase. This keeps the Cohort entity simple (see [cohort.md](../../03-data-model/cohort.md)) while preserving the field space to add drip later.

### Q27 — Lesson prerequisites (must finish Lesson 1 before Lesson 2)?

**Source:** [core-concepts.md](../../01-domain/core-concepts.md) · **Impact:** Medium (progress + navigation logic)

- **Pros (of sequential gating):** Enforces intended learning order; clearer progress model.
- **Cons:** Introduces a dependency graph, "locked lesson" states, and edge cases (what if a prerequisite quiz is failed?).

**Recommendation:** **MVP: linear ordering only, no hard gating** — lessons display in order and are all openable; completion is tracked but not enforced as a prerequisite. Add optional "require completion to advance" as a later, per-course setting. Keeps the completion rule (see [Q_completion](#q-completion)) tractable.

### <a id="q-completion"></a>Q-completion — How exactly is course completion computed? *(Business logic gap)*

**Source:** [core-concepts.md](../../01-domain/core-concepts.md), [mvp.md](../../07-roadmap/mvp.md) · **Impact:** Very high (the core learning outcome)

> The docs say completion = "required lessons read + passing graded quizzes" but never define *which lessons are required*, *how percentages are computed*, or *whether practice quizzes count*.

- **Pros (of an explicit, simple rule):** Deterministic certificates and progress bars; testable acceptance criteria.
- **Cons:** Any rule chosen constrains the Lesson/Quiz/Progress entities.

**Recommendation:** Define MVP completion precisely as: **a course is complete when (a) every lesson marked `required = true` has a Progress record with `completed = true`, and (b) every *graded* quiz in the course has a passing Quiz Submission.** Practice quizzes never affect completion. Course % = (completed required lessons + passed graded quizzes) / (total required lessons + total graded quizzes). Add a `required` boolean to the **Lesson** entity ([lesson.md](../../03-data-model/lesson.md)). This must be resolved before building progress tracking or certificates.

### Q45 — Manual reset of graded-quiz attempts after all attempts fail?

**Source:** [mvp.md](../../07-roadmap/mvp.md), [core-concepts.md](../../01-domain/core-concepts.md) · **Impact:** Medium

- **Pros (of allowing instructor/coordinator reset):** Humane; avoids a learner being permanently stuck; realistic for training.
- **Cons:** Needs a permissioned action + audit trail on an otherwise-immutable submission history.

**Recommendation:** **Yes — allow Course Authors, Cohort Coordinators, and Learning Facilitators to reset a learner's graded-quiz attempts**, recorded as an auditable action (who reset, when, why). This is a small feature that removes a hard dead-end. Specify it in [quiz-engine.md](../../02-features/quiz-engine.md).

### Q28 — How long are quiz scores retained?

**Source:** [core-concepts.md](../../01-domain/core-concepts.md) · **Impact:** Low–Medium (storage, privacy)

- **Pros (of indefinite retention):** Certificates and audits depend on submission history; simplest policy.
- **Cons:** Ties into data-privacy/GDPR ([Q57](#q57--data-privacy-retention-and-gdpr)); indefinite PII retention may be non-compliant for some buyers.

**Recommendation:** **Retain indefinitely by default**, but treat retention as **configurable** and fold it into the data-privacy policy ([Q57](#q57--data-privacy-retention-and-gdpr)). Certificates should snapshot the data they need so scores can be purged without invalidating a certificate.

### Q29 — Do forums support rich text and image uploads?

**Source:** [core-concepts.md](../../01-domain/core-concepts.md) · **Impact:** Medium (forum feature + storage + moderation)

- **Pros (of rich text + images):** Better Q&A (code blocks, screenshots); higher engagement.
- **Cons:** Image uploads pull in object storage, size limits, and moderation of uploaded content; XSS surface for rich text.

**Recommendation:** **MVP: Markdown text only** (the Forum Post entity already specifies Markdown), rendered through a strict sanitizer. Defer image/file attachments (and the related [Q32](#q32--separate-media-asset-entity) Media Asset entity) to a later phase. See [course-and-module-forums.md](../../02-features/course-and-module-forums.md).

### Q30 — When a learner is deactivated to free a seat, do they lose progress/records?

**Source:** [core-concepts.md](../../01-domain/core-concepts.md) · **Impact:** High (data integrity + licensing)

- **Pros (of soft-deactivation that preserves records):** Certificates, audit trails, and re-activation stay intact; only the *active seat count* is affected.
- **Cons:** Requires a clear definition of "active" seat that excludes deactivated users while retaining their data.

**Recommendation:** **Deactivation is a status change, not a delete.** A deactivated learner keeps all progress, submissions, and certificates but **does not count against active seats** and cannot log in. This resolves the tension in [Q33](#q33--soft-delete-vs-hard-delete) and defines "active seat" for [ADR-3](pre-baseline-adrs.md). Reactivation restores access if a seat is available.

---

## 4. Data Model

### Q31 — Store per-question answer selections, or just the final score?

**Source:** [initial-entities.md](initial-entities.md) · **Impact:** Medium

- **Pros (of storing per-question answers):** Enables review screens ("here's what you got wrong"), item analysis, dispute resolution, and audit trails.
- **Cons:** More storage and a `quiz_submission_answers` table; slightly more write complexity.

**Recommendation:** **Store per-question selections.** The storage cost is trivial and the capability (answer review, analytics, audits) is expected by users and is painful to reconstruct later. Model a `Quiz Submission Answer` child of [quiz-submission.md](../../03-data-model/quiz-submission.md).

<a id="q32--separate-media-asset-entity"></a>
### Q32 — Separate "Media Asset" entity for uploads?

**Source:** [initial-entities.md](initial-entities.md) · **Impact:** Low for MVP (depends on Q29)

- **Pros:** A single asset table centralizes storage keys, ownership, and cleanup for any future upload (forum images, certificate logos, avatars).
- **Cons:** Unneeded if MVP has no user uploads beyond an org logo.

**Recommendation:** **Not needed for MVP** given Markdown-only forums (Q29). Introduce a `MediaAsset` entity when the first real upload feature lands (forum images or lesson media). Note the org logo can be a single configured object-storage key without a full entity.

<a id="q33--soft-delete-vs-hard-delete"></a>
### Q33 — Soft-delete vs. hard-delete for seat aggregation?

**Source:** [initial-entities.md](initial-entities.md) · **Impact:** High (see Q30)

- **Pros (of soft-delete):** Preserves learning records and certificates; makes "active seat" a filter (`status = active`), not a row count; reversible.
- **Cons:** Every query must respect the soft-delete flag; risk of leaking deactivated users if a filter is missed.

**Recommendation:** **Soft-delete (status field + `deleted_at`) for User and Organization**; the active-seat count is `COUNT(users WHERE status = 'active')`. Aligns with Q30. Hard-delete only via an explicit GDPR erasure path ([Q57](#q57--data-privacy-retention-and-gdpr)).

---

## 5. Frontend & UX

### Q34 — Shared navigation with conditional items, or separate per-role layouts?

**Source:** [navigation.md](../../05-frontend/navigation.md) · **Impact:** Medium

- **Pros (of one shell + role-conditional items):** One layout to build and maintain; smooth for multi-role users (Q22); consistent shell.
- **Cons:** Conditional logic can sprawl; risk of showing items a role can't use.

**Recommendation:** **Single app shell with role-driven navigation** rendered from the user's permission set. Given the multi-role direction (Q22) and single-binary SPA (ADR-1), one adaptive shell is clearly right. Drive nav items from the same permission data that governs the API.

### Q35 — Where does search live?

**Source:** [navigation.md](../../05-frontend/navigation.md) · **Impact:** Low–Medium

- **Pros (of a scoped search in-context, e.g., within course catalog and forums):** Simple; Postgres full-text is enough at MVP scale.
- **Cons:** A global omni-search is more work and less useful when the catalog is small.

**Recommendation:** **MVP: scoped search only** — course/cohort list filtering and forum thread search, backed by Postgres full-text. Defer global omni-search. See [Q56](#q56--search-approach).

### Q36 — Global notification center?

**Source:** [navigation.md](../../05-frontend/navigation.md) · **Impact:** Medium (ties to notifications, Q55)

- **Pros:** Central place for enrollment, forum replies, completion, deadline reminders; improves engagement.
- **Cons:** Requires the notifications subsystem ([Q55](#q55--notifications--email)) to exist first.

**Recommendation:** **MVP: email notifications for the few critical events** (invite, cohort start, forum reply, completion). Add an **in-app notification center in a fast-follow phase** once the notifications backend (Q55) exists. Don't build the bell icon before the backend.

### Q37 — Breadcrumbs for deep navigation?

**Source:** [navigation.md](../../05-frontend/navigation.md) · **Impact:** Low

- **Pros:** Course → Module → Lesson is naturally deep; breadcrumbs aid orientation.
- **Cons:** Minor; pure UI polish.

**Recommendation:** **Yes, include breadcrumbs in the course player** — the hierarchy is deep enough to warrant them, and they're cheap. Not a blocker; a UI detail to note in screen specs.

---

## 6. Architecture & Engineering

### Q38 — Adopt a named architectural pattern (hexagonal, clean, etc.)?

**Source:** [architecture-principles.md](../../06-architecture/architecture-principles.md) · **Impact:** Medium (codebase structure)

- **Pros (of a light layered/hexagonal approach):** Clear separation (handlers → services → repositories) aids testing and future SaaS/self-hosted divergence.
- **Cons:** Dogmatic clean-architecture in Go often over-abstracts; ceremony without payoff at this size.

**Recommendation:** **Adopt a pragmatic layered structure** — HTTP handlers, a service/domain layer, and a repository layer over Postgres — without full hexagonal ceremony. This satisfies Principle 2 (separate concerns) and Principle 1 (keep it simple). Record as an ADR once agreed.

> **Proposed resolution:** [ADR-6 — Modular Monolith Architecture](pre-baseline-adrs.md#adr-6-modular-monolith-architecture) (layered structure *within* enforced per-module boundaries).

### Q39 — Testing strategy (unit / integration / e2e)?

**Source:** [architecture-principles.md](../../06-architecture/architecture-principles.md) · **Impact:** High (must be decided before code)

- **Pros (of a defined pyramid early):** Prevents untested business logic (grading, licensing, completion) from shipping; CI stays meaningful.
- **Cons:** Requires up-front investment in test infrastructure (test DB, fixtures).

**Recommendation:** Define now: **unit tests for domain logic** (grading, completion, license verification), **integration tests against a real Postgres** (via testcontainers or a disposable DB) for repositories and handlers, and a **small e2e smoke suite** for the critical learner path. The licensing and grading logic in particular are too important to leave untested. Record as an ADR.

### Q40 — How are database migrations managed?

**Source:** [architecture-principles.md](../../06-architecture/architecture-principles.md) · **Impact:** Very high (self-hosted upgrade path)

- **Pros (of embedded, versioned migrations run at boot):** A self-hosted binary can migrate the customer DB automatically on upgrade — critical for the single-binary promise.
- **Cons:** Auto-migrate-on-boot has risk (a bad migration on a customer DB); needs a backup/guard story ([Q58](#q58--backup--disaster-recovery)).

**Recommendation:** **Embed versioned SQL migrations in the binary** (e.g., a `golang-migrate`/`goose`-style tool with `go:embed`) and run them at startup behind a config flag, with a clear log and a pre-migration backup recommendation. This is essential to [Q54](#q54--self-hosted-upgrade-path). Record as an ADR — this is one of the most important engineering decisions for a self-hosted product and is currently undocumented.

> **Proposed resolution:** [ADR-10 — Database Migrations](pre-baseline-adrs.md#adr-10-database-migrations--embedded-run-at-startup). Note the *mechanism* is now .NET (EF Core Migrations / DbUp), not the Go tooling named above — the *principle* (embedded, run at startup behind an advisory lock, forward-compatible) is unchanged.

### Q63 — Which deployment manifests does s2c officially support and maintain?

**Source:** [ADR-5](pre-baseline-adrs.md#adr-5-container-image-delivery-model) · **Impact:** Medium (support surface)

- **Pros (of a single supported path, Docker Compose):** Smallest support surface; one manifest to keep in sync; covers most single-node self-hosters.
- **Cons:** Larger customers on Kubernetes will want a Helm chart; supporting Compose + Helm + SaaS from one codebase is real, ongoing effort (GitLab maintains a chart↔version map; Sentry ships no official chart and fragments onto community charts).

**Recommendation:** Treat **Docker Compose as the officially-supported baseline** and offer **Helm as a supported-but-secondary path**; document that raw/hand-rolled K8s is community-supported. Revisit if enterprise K8s demand grows. See [ADR-5](pre-baseline-adrs.md#adr-5-container-image-delivery-model).

### Q64 — What enforces modular-monolith boundaries in the .NET codebase?

**Source:** [ADR-6](pre-baseline-adrs.md#adr-6-modular-monolith-architecture) · **Impact:** Medium (long-term maintainability)

- **Pros (of automated enforcement):** Prevents the "modular monolith → big ball of mud" decay; a boundary violation fails the build, not just review.
- **Cons:** Up-front setup; some ceremony.

**Recommendation:** Enforce boundaries with **project/assembly separation + `internal` visibility** and **architecture tests** (e.g., ArchUnitNET / NetArchTest) run in CI. Ban cross-module DB joins via table-ownership rules. Confirm the exact tooling at implementation time.

### Q65 — Confirm backend language (.NET) against team fluency and hiring

**Source:** [ADR-7](pre-baseline-adrs.md#adr-7-backend-language--net-aspnet-core) · **Impact:** High (velocity, hiring)

- **Context:** [ADR-7](pre-baseline-adrs.md#adr-7-backend-language--net-aspnet-core) proposes **.NET (ASP.NET Core)** because container delivery neutralizes Go's single-binary advantage while .NET's batteries-included ecosystem lowers friction for a feature-heavy LMS.
- **Open point:** This should be confirmed against the actual team's C#/.NET fluency and local hiring market. If the team is materially stronger in Go, ADR-7 flags Go as a reasonable fallback.

**Recommendation:** Confirm .NET with whoever owns delivery; otherwise revisit ADR-7 before implementation begins.

### Q67 — Vendor control plane: scope & boundary

**Source:** [ADR-12](pre-baseline-adrs.md#adr-12-vendor-control-plane-separation) · **Impact:** High (system boundary)

- **Context:** License *issuance*, cross-organization management, and subscription/billing live in a **separate vendor control plane**, not the bundled LMS ([ADR-12](pre-baseline-adrs.md#adr-12-vendor-control-plane-separation)). On a self-hosted instance there is only one Organization, so these vendor concerns cannot (and should not) live in the customer's application. This planning repo models the **LMS side of the contract** only.
- **Open points:**
  - The exact **license-file schema** the LMS consumes (fields, signature scheme, key rotation).
  - The **seat-usage telemetry** contract (payload, cadence, and opt-out for air-gapped installs).
  - Whether the control plane lives in a **separate repo** (recommended) and how the two evolve in lockstep.

**Recommendation:** Keep the control plane a **separate application/repo**; in this repo specify only the two contract surfaces (license-file schema in, telemetry out). Define the license-file schema before building the Licensing module ([ADR-6](pre-baseline-adrs.md#adr-6-modular-monolith-architecture)).

---

## 7. Cross-Cutting Concerns Not Yet Addressed

> These are **not currently mentioned anywhere** in the planning docs but are hard prerequisites for a shippable product. Each is raised here so it can be designed deliberately.

### Q50 — Authentication & session model *(MVP blocker)*

- **Pros (of session cookies backed by Redis):** Redis is already in the stack (Q13); server-side sessions are simple to revoke; good fit for a same-origin embedded SPA (ADR-1).
- **Cons:** Cookies need CSRF protection; JWTs are more "API-native" but harder to revoke.

**Recommendation:** **Redis-backed HTTP-only session cookies** (not JWTs) for the first-party SPA — revocation, "log out everywhere," and simplicity all favor sessions given the architecture. Define password policy, hashing (argon2id/bcrypt), reset flow, and the invite-acceptance token flow. This must be specified before any protected endpoint is built.

> **Proposed resolution:** [ADR-11 — Authentication (Redis-Backed Session Cookies)](pre-baseline-adrs.md#adr-11-authentication--redis-backed-session-cookies), realized via ASP.NET Core cookie authentication + Redis.

### Q51 — Authorization enforcement model *(MVP blocker)*

- **Pros (of centralized middleware + per-resource ownership checks):** The permission matrix already exists; enforcing it in one place prevents drift.
- **Cons:** Resource-level checks (own course, own cohort, org-scoped) need consistent plumbing.

**Recommendation:** Enforce role permissions in **middleware**, and resource ownership/tenancy in the **service layer** via explicit checks. Derive both API and nav ([Q34](#q34--shared-navigation-with-conditional-items-or-separate-per-role-layouts)) from one permission definition. Specify before protected features.

<a id="q53--multi-tenancy-isolation-strategy"></a>
### Q53 — Multi-tenancy isolation strategy *(expensive to reverse — decide early)*

- **Pros (of a `organization_id` column on every tenant-scoped row, single DB):** Works identically in single-tenant (one org row) and SaaS (many); simplest ops; matches ADR-2.
- **Cons:** Relies on every query filtering by `organization_id` (leak risk); noisy-neighbor concerns at large SaaS scale.

**Recommendation:** **Row-level tenancy via `organization_id` on all tenant-scoped tables from day one**, even in single-tenant mode (one org). Enforce it centrally (a scoped query helper / RLS). This is the reversible-by-default choice (Principle 5) and prevents the worst retrofit in the whole plan. Ties directly to [Q7](#q7--is-this-a-hosted-saas-or-self-hosted-product).

> **Proposed resolution:** [ADR-9 — Multi-Tenancy Isolation via `organization_id` + Postgres RLS](pre-baseline-adrs.md#adr-9-multi-tenancy-isolation-via-organization_id--postgres-rls).

<a id="q54--self-hosted-upgrade-path"></a>
### Q54 — Self-hosted upgrade path *(MVP-adjacent blocker)*

- **Pros (of "drop-in new binary + auto-migrate"):** Delivers on the single-binary promise; trivial for customers.
- **Cons:** Requires migration safety ([Q40](#q40--how-are-database-migrations-managed)) and backups ([Q58](#q58--backup--disaster-recovery)).

**Recommendation:** Document the upgrade story explicitly: **stop old binary → back up DB → start new binary (runs embedded migrations) → verify health check.** A product whose whole pitch is easy self-hosting must have a first-class, documented upgrade path. Pair with Q40.

### Q55 — Notifications & email

- **Pros (of a small event → email system for MVP):** Several journeys (invite, completion, forum reply) assume email; a mailer env var is already planned (ADR-2).
- **Cons:** Deliverability, templating, and an in-app center ([Q36](#q36--global-notification-center)) are scope.

**Recommendation:** **MVP: transactional email only** via a configurable SMTP/provider, for a short list of events (invite, cohort start, forum reply, course completion). Model a notification event internally so an in-app center can be added later without rework.

### Q56 — Search approach

**Recommendation:** **Postgres full-text search** for MVP (course/cohort/forum). No external search engine (Elasticsearch/Meilisearch) until scale or relevance needs demand it. Reinforces the single-binary simplicity goal. (Related: [Q35](#q35--where-does-search-live).)

### Q57 — Data privacy, retention, and GDPR

- **Pros (of building export + erasure early):** "Data sovereignty" is a core selling point; regulated buyers will ask; cheap to design in, costly to retrofit.
- **Cons:** Erasure conflicts with immutable certificates/audit trails — needs careful snapshotting.

**Recommendation:** Define a **data-retention & erasure policy**: per-user export, a documented erasure path that anonymizes rather than breaks referential integrity, and configurable retention for quiz/forum data ([Q28](#q28--how-long-are-quiz-scores-retained)). Add a **data-privacy product principle** ([Q42](#q42--add-a-data-privacy-principle)). Elevate before handling real PII.

### Q58 — Backup & disaster recovery

**Recommendation:** Since self-hosters own their data, ship **documented `pg_dump`-based backup guidance and a pre-upgrade backup prompt** rather than a bespoke system. A product selling data sovereignty must give customers a credible backup/restore story even if the mechanism is "use Postgres' own tools."

### Q59 — Observability (logging, metrics, health checks)

**Recommendation:** Principle 5 (architecture) mandates this. **MVP: structured logs, a `/healthz` endpoint, and Prometheus-style metrics** exposed optionally. Keep it single-binary-friendly (no mandatory external collector).

### Q60 — CI/CD pipeline

**Recommendation:** Per architecture Principle 6, stand up CI **before feature work**: build the binary (with embedded SPA), run the test pyramid ([Q39](#q39--testing-strategy-unit--integration--e2e)), and produce release artifacts. Cheap now, painful to add mid-stream.

### Q61 — Non-functional / performance targets

**Recommendation:** Turn the "1,000–10,000, architect for 100,000" scale goal into **concrete SLOs** (e.g., p95 page/API latency, concurrent-learner target on defined baseline hardware) so the architecture and load tests have a target. Currently there is a scale aspiration but no measurable bar.

### Q62 — Rate limiting, abuse protection, and audit logging

**Recommendation:** **MVP: basic rate limiting on auth endpoints** and an **audit log for sensitive actions** (user management, seat changes, quiz-attempt resets, license load). The Auditor-role question ([Q23](#q23--read-only-auditor-role)) implies audit demand; a minimal audit log now supports it later.

---

## 8. Product Principles

### Q41 — Add an accessibility principle?

**Recommendation:** **Yes.** "Learner experience is the priority" (Principle 3) logically entails accessibility. Add a principle committing to WCAG 2.1 AA as a baseline; it also strengthens sales to public-sector/education buyers.

### Q42 — Add a data-privacy principle?

**Recommendation:** **Yes** — see [Q57](#q57--data-privacy-retention-and-gdpr). Given the data-sovereignty positioning, a privacy principle is on-brand and should be explicit.

### Q43 — Industry-specific principles?

**Recommendation:** **Not yet.** The product is deliberately general-purpose ([Q3, Resolved](#resolved)). Revisit if a vertical (compliance training, higher-ed) becomes a focus.

---

## 9. Delivery & Roadmap

### Q18 — Are cohort-level analytics exposed to Learning Coordinators or only Organization Managers?

**Source:** [vision.md](../../00-product/vision.md) · **Impact:** Medium

- **Pros (of exposing cohort analytics to Coordinators):** Coordinators own cohort success; withholding completion metrics undercuts their role (they already "monitor progress").
- **Cons:** Org-wide/cross-cohort analytics are broader and arguably manager-only.

**Recommendation:** **Coordinators see analytics scoped to their own cohorts; Organization Managers see org-wide/cross-cohort analytics.** This matches the existing role definitions and the permission matrix. Confirm and close.

### Q47 — Can learners bookmark/save courses for later?

**Recommendation:** **Defer.** In a cohort-assignment model (Q2b), a browsable "save for later" catalog isn't part of MVP. Revisit with self-paced enrollment.

### Q48 — Instructor approval workflow before publishing?

- **Pros (of an Admin approval gate):** Quality control on shared platforms.
- **Cons:** Overhead; unnecessary for internal-s2c MVP where Course Authors are trusted staff.

**Recommendation:** **No approval workflow for MVP** (Course Authors publish directly). Add an optional review gate later if multi-org/marketplace scenarios emerge.

### Q50r / Q51r / Q52 — Phase durations, team size, external deadlines

**Source:** [phases.md](../../07-roadmap/phases.md) · **Impact:** Planning

**Recommendation:** These are **program-management inputs, not product decisions** — they should be filled in by whoever owns delivery. Development can begin defining Phase 1 scope (= the MVP) without them, but the phase plan in [phases.md](../../07-roadmap/phases.md) should be completed before committing to dates.

---

## Resolved

| # | Question | Answer | Rationale | Resolved |
|---|----------|--------|-----------|----------|
| Q1 | Is the LMS for individuals, organizations, or both? | Primarily for organizations (B2B / Single-Tenant Self-Hosted or multi-tenant SaaS modes). | Follows the environment-configurable single-binary architecture to let companies run their own infrastructure. | June 2026 |
| Q2 | Will learning be self-paced, cohort-based, instructor-led, or hybrid? | Hybrid. For now, focus on cohort-based learning, but design for all. | Support cohort-based learner interaction, group forums, and coordinator management for the initial courses. See [Q2b](#q2b--reconcile-cohort-based-vs-self-paced-learner-journey-consistency-conflict). | June 2026 |
| Q3 | Is this a general-purpose LMS or domain-specific? | Start general-purpose, but architect for domain extensibility. | Allows wide initial usability while facilitating plugin/add-on architectures later. | June 2026 |
| Q4 | What is the initial target scale? | Design for 1,000–10,000 active learners, but architect for 100,000. | Satisfies immediate organization demands while ensuring the architecture handles growth. See [Q61](#q61--non-functional--performance-targets) for concrete SLOs. | June 2026 |
| Q5 | Will the platform support paid courses? | Not for MVP (internal s2c use), but paid courses will be supported in the long run. | MVP targets internal s2c workflows; monetization features can follow in later phases. | June 2026 |
| Q6 | Will there be a subscription model? | Yes, in the long run. | Supports ongoing commercialization paths for hosted organizations. | June 2026 |
| Q8 | Will there be assessments? | Yes, both practice and graded quizzes. | Essential for assessing and validating learner knowledge/progress. | June 2026 |
| Q9 | Will certificates be generated? | Yes, certificates will be issued on course completion. | Required for professional validation and completion tracking. | June 2026 |
| Q10 | Will organizations manage learners? | Yes, organization managers can manage users, track progress, and configure seats. | Supports B2B and business-unit-level administration. | June 2026 |
| Q11 | What content types are supported? | Extensible content structure. MVP will support Rich Text, Required Readings (with external links), and Lesson Notes. | Limits initial scope while maintaining modular content players. | June 2026 |
| Q12 | Will there be forums? | Yes, Course-level and Module-level forums. | Promotes learner interaction, feedback, and engagement. | June 2026 |
| Q13 | What tech stack will be used? | Go, Postgres, Redis, Object Storage, and Vite + React SPA embedded in Go. Pragmatic React use alongside native browser APIs. | Ensures single-binary deployment ease for self-hosting, high performance, and rapid UI development. | June 2026 |
| Q14 | Should the system support SCORM/xAPI? | No for MVP; document an integration strategy for a later phase. | Compliance adds excessive early-stage complexity; a clean, internal API progress tracker is sufficient. | June 2026 |
| Q15 | What is the MVP? | s2c managing courses, modules, lessons (readings, notes), users, progress, course/module forums, and quizzes. | Delivers a complete, useful e-learning platform for internal s2c needs. | June 2026 |

---

## How to Use This Document

1. **Adding a question:** Add it under the appropriate themed section with a unique `Q#`, its source document, impact, and — where possible — Pros, Cons, and a Recommendation.
2. **Resolving a question:** Move the row to the **Resolved** table with the answer, rationale, and resolution date, and update every document affected by the decision.
3. **Referencing in other docs:** Use `See [Q30](pre-baseline-questions.md#q30--when-a-learner-is-deactivated-to-free-a-seat-do-they-lose-progressrecords)` (or just `[Q30]`) so local docs point back here rather than restating the question.
4. **Consistency conflicts:** Questions marked ⚠️ indicate two documents currently disagree. Resolve these first — they block downstream work.

---

## Related Documents

- [Vision](../../00-product/vision.md)
- [Goals](../../00-product/goals.md)
- [Target Users](../../00-product/target-users.md)
- [MVP Definition](../../07-roadmap/mvp.md)
- [Architecture Decisions](pre-baseline-adrs.md)
