# Consolidation and implementation-readiness assessment

> **Status:** Foundation ready; feature and release gates remain\
> **Authority:** Supporting assessment, not a replacement product specification\
> **Baseline decisions:** 2026-09-17; final validation 2026-09-18

## Finding

**Assessment scope:** This report records the documentation consolidation committed as `1a3b3b9`. Implementation began afterward. See the [implementation plan](implementation-plan.md) and [local guide](../06-architecture/local-development.md) for current executable scope and checks; the consolidation-only validation record below remains historical evidence.

**Foundational implementation can safely begin.** Stack, distribution boundary, privilege delegation, learner-seat accounting and learning-run identity are settled. No genuine foundational blocker remains. This is not a claim that every feature is fully specified, that an application exists, or that production readiness has been demonstrated.

The repository now represents **Variable LMS**, a product of **Variable** initially serving **s2c's internal training**. No application features, migrations, package manifests, containers or CI configuration were implemented in this consolidation.

## Architecture baseline

.NET 10 LTS / ASP.NET Core modular monolith, React/Vite static web application and PostgreSQL. Business modules own writes and private implementations; explicit read-only joins and ordinary local transactions are allowed. Redis, object storage, brokers and Kubernetes are not universal dependencies. Sessions and durable work use PostgreSQL initially. Sensitive state/audit/work intent commit atomically; delivery retries are idempotent at the business boundary.

Versioned OCI images plus versioned deployment packages support customer-controlled upgrades. Compose is the initial packaging path; supported Kubernetes delivery uses Helm when required. Runtime and migration credentials are separate; migration is an explicit upgrade step. Backup includes database, persistent files, license/configuration and protected keys. Shared-database SaaS and service extraction are future decisions driven by actual requirements.

## Decisions encoded

| Accepted decision | Authoritative location and implementation elaboration |
|---|---|
| Variable/company, Variable LMS/product, s2c first customer | [D1](../00-product/accepted-decisions.md), [vision](../00-product/vision.md), root README |
| .NET 10/ASP.NET Core/React/Vite/PostgreSQL/OCI | D2; [ADRs 5, 7, 8](../06-architecture/decisions.md) |
| Modular now, possible extraction later | D3; ADR-6; [module boundaries](../06-architecture/module-boundaries.md) |
| Distribution independent of application topology | D4; ADR-5/10; [release and operations](../06-architecture/release-and-operations.md) |
| Explicit privileged-role grants | D5; [single grant/revoke matrix](../01-domain/roles-and-permissions.md); [identity security](../06-architecture/security-and-identity.md) |
| Learner-entitlement seats and extensible license | D6; [core concepts](../01-domain/core-concepts.md); [license contract](../04-api-design/license-contract.md); provisioning/User/Organization |
| Enrollment owns learning history | D7; core concepts; Enrollment/Progress/Quiz Submission/Certificate; quiz and completion features |

Engineering realizations are identified as such: PostgreSQL ticket storage, dedicated reauthenticated Administrator operation, module transactions, allowance epochs, invalid empty-course publication and JWS/ES256 wire profile. They implement settled behavior; they do not choose unresolved product scoring, retention or access semantics.

## ADR reconciliation

All 12 prior ADRs were individually reviewed; [the decision log](../06-architecture/decisions.md) holds current decisions and material reasoning. The original working-tree text is preserved in [history](../08-research/history/pre-baseline-adrs.md).

| Disposition | ADRs and reason |
|---|---|
| Superseded | 1: Go/single-binary direction replaced by explicitly accepted .NET/OCI architecture. |
| Accepted | 4: SCORM/xAPI exclusion remains sound, no promised future phase. 8: React/Vite SPA matches the chosen stack. |
| Accepted with modification | 2: typed configuration/secrets, no SaaS toggle. 3: precise learner counting/extensible license. 5: immutable release packages/customer control. 6: joins/local transactions permitted. 7: .NET 10, no Go fallback/AOT or benchmark assumption. 9: single-organization ownership, no mandatory shared-tenant RLS. 10: explicit migration phase/restricted runtime. 11: revocable cookies, Redis optional. 12: separate Variable issuer, no mandatory telemetry/provisioner. |
| Newly Accepted | 13: local atomicity and durable effects required for reliable audit/completion/certificates/notifications. |
| Rejected / Still Proposed | No entire prior ADR falls in these states. Rejected clauses are recorded in modified ADRs; unsupported future capabilities stay explicitly deferred/unapproved, not silently accepted. |

## Documentation reconciliation

- Source precedence, metadata and task routes now live in [one source map](../README.md). Canonical features/entities reference one role policy; the dossier explicitly remains derived.
- Stale resolved questions and advisory recommendations moved out of current requirements. The historical register preserves reasoning; the current register contains actual local/release gates and engineering discretion.
- Old browse/self-enrollment, sequential unlock, publication approval and notification-center journeys were replaced with assigned-cohort flows. Published internal course selection for staff is not a public learner catalog.
- Every organization-owned entity now declares organization ownership; pending invitation state, required lesson flag, course/run consistency, immutable submission snapshots and allowance-reset semantics are explicit.
- Progress, attempts and completion attach to Enrollment. Re-enrollment no longer overwrites global user/course evidence. Certificate issuance remains once/user/course; verification wording no longer promises an anonymous endpoint or treats an opaque ID as cryptographic proof.
- Seat counts exclude staff-only, pending and deactivated accounts. Privileged changes cannot bypass role delegation through profile/email/status endpoints. Required audit is transactional, replacing best-effort wording.
- Active architecture/dossier text no longer mandates Redis/RLS/startup DDL/SaaS/NativeAOT or claims unmeasured throughput, instant upgrades or already signed image releases.
- Historical ADRs/questions/entity sketch and architecture research remain visible with supersession markers. Existing uncommitted planning material was inspected and retained in snapshots where it held architectural rationale; unrelated workspace changes were not discarded.

## Repository operating model

Start at root README → [AGENTS.md](../../AGENTS.md) → [source map](../README.md). Read the accepted decisions, then only the owning feature/domain/ADR and its linked entities/questions. CLAUDE.md is a thin wrapper; future scoped instructions belong near implemented code when there is actual guidance to give. Root instructions reference [enforceable invariants](../06-architecture/implementation-invariants.md) rather than repeating an encyclopedia.

Update canonical behavior, representation/contracts, implementation and meaningful tests together. A historical document, proposed paragraph or derived page cannot override an accepted decision. Resolve genuine disagreement explicitly. The repository will contain implementation/tests/deployment/operations alongside product truth, but this pass creates only documentation and dossier changes.

## Remaining issues

| Classification | What remains | Effect |
|---|---|---|
| True implementation blockers | **None** for foundational work | Begin organization/authentication/module/persistence foundations. |
| Feature-local decisions | Q68 license lifecycle; Q69 window/withdrawal/archive access; Q70 reading signal; Q71 multi-select; Q72 attempt lifecycle; Q73 feedback/preview; Q74 live edits; Q75 administrative reissue | Resolve before implementing the affected semantics. Reading/attempt/feedback choices gate later walking-skeleton steps, not initial foundations. |
| Feature-local conditional scope | Q79 transfer | Do not implement unless required; explicit semantics needed if adopted. |
| Feature/release acceptance | Q57 privacy/retention before real personal data; Q61 performance/recovery targets; Q63 first supported deployment platforms; Q41 formal accessibility target; Q78 distribution terms | No claim of production/commercial release readiness until closed. |
| Engineering decisions | Libraries, concrete schemas/query indexes, API shapes, build/registry tooling, test fixtures, PDF rendering, worker tuning | Competent implementers choose within recorded boundaries and validate evidence. No new product meeting required. |

The [question register](../00-product/open-questions.md) owns exact questions and gates. Commercial prices and staffing/dates remain outside the LMS architectural blocker list.

## First implementation sequence

[Detailed plan](implementation-plan.md): controlled organization/Admin bootstrap and authenticated shell → author/publish one course → schedule/staff cohort → verify license/invite/activate Learner → assign new run/read required content → submit graded assessment → evaluate completion/issue certificate → expand remaining MVP and qualify versioned release/recovery. Each slice includes real persistence, API/UI, negative authorization, races and failure/retry tests. No entity-first/API-second/UI-last plan is proposed.

## Changed areas

Root entrypoints/source map; accepted decisions/product/domain; seven feature specs; entity index and 21 entity/deferred-entity specs; API/license and UI guidance/templates; ADRs/boundaries/security/operations/invariants; MVP/readiness/implementation sequence; historical research and four derived dossier pages. CSS/theme assets remain presentation-only and are not the LMS application.

## Validation record

Final checks were completed on 2026-09-18. These validate the repository documentation; they do not establish application/database runtime correctness.

- Checked **77 Markdown/HTML documents and 887 local file/anchor references**, including archived material and dossier links: zero missing files or anchors.
- Checked all four dossier HTML documents for balanced elements, one main landmark, one primary heading and explicit derived-source markers; no structural errors. This was a structural/content check, not a browser visual/accessibility certification.
- Verified metadata on every document under docs, organization ownership in current entity contracts, Enrollment keys on raw progress/attempts, and a single active permission-matrix source.
- Verified ADR IDs 1–13 and each disposition; checked current references against historical Go, mandatory Redis/RLS, startup migration, global learning-key, primary-role, certificate-verification and stale MVP claims. Remaining historical/retired wording is explicitly marked.
- Reviewed role delegation and indirect escalation, atomic learner-seat paths, re-enrollment versus certificate uniqueness, immutable reset evidence, module dependency direction and migration/restore rules across their canonical sources.
- Verified the shared AGENTS entrypoint and thin CLAUDE wrapper; inspected Git history and the pre-existing working tree before rewriting. Existing staged .gitignore and unrelated .DS_Store files were not changed by this pass.
- `git diff --check` passes. A separate comparison with the captured pre-consolidation files covers untracked documents that Git diff omits: **64 existing files updated and 14 documentation files added**. No product source, runnable migrations, package manifests, CI or deployment configurations were added. The only stylesheet change is its product-name comment; theme.js is unchanged.
- Verified relevant technical claims against the primary sources in the reference document: .NET support, PostgreSQL RLS, ASP.NET Core sessions/key persistence, EF migration/AOT limitations, Compose/Helm and JWS/ES256. Historical external research links were not all revalidated.
- No application tests were run because this repository still contains no application implementation. Planned behavioral, authorization, concurrency, architecture and recovery tests are specified in the invariants and implementation sequence.

## Related documents

[Accepted decisions](../00-product/accepted-decisions.md) · [ADRs](../06-architecture/decisions.md) · [MVP](mvp.md) · [Technical sources](../08-research/references.md)
