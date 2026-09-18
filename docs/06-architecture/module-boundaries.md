# Module ownership and communication

> **Status:** Confirmed baseline\
> **Authority:** Canonical — elaborates ADR-6 and ADR-13\
> **Updated:** 2026-09-18

## Purpose

Keep the monolith understandable and enforce business ownership without imitating a distributed system.

## Ownership map

| Module | Owns writes/state | Public capabilities and dependencies |
|---|---|---|
| Identity & Organization | users, roles, credentials, sessions, organization identity/branding; audit persistence facility | Authenticate, evaluate account authority, manage users/roles; calls Licensing capacity policy for consuming changes |
| Licensing | verified license document/projection and organization capacity guard protocol | Verify/load license; evaluate learner-capacity changes inside caller's transaction; no account CRUD or billing |
| Course Authoring | courses, authorship grants, modules, lessons, published requirements | Author/publish, expose course/lesson requirement snapshots; quiz content is delegated to Assessment |
| Enrollment & Cohorts | cohorts, staff grants, enrollments, enrollment lifecycle | Launch/assign; expose run context and staff scopes; record completion through its own command interface |
| Assessment | quiz definitions, immutable submitted attempts/answers and reset/allowance records | Author quizzes, accept/score attempts, return passing evidence for a run; uses run context and authoring ownership |
| Progress & Certification | lesson progress, completion evaluation orchestration, certificates | Record progress; evaluate requirements and attempt evidence; call Enrollment to mark completed; issue unique credential |
| Discussion | cohort-scoped threads/posts and moderation | Read/write forums under cohort grants; enqueue reply notification intent |
| Notifications | durable email intents, delivery attempts, retry scheduling | Enqueue within producer's transaction; render and send after commit |

The audit facility records another module's action but does not own that module's business transition. Completion evidence is stored with the Enrollment completion state via Enrollment's interface; a separate achievement/completion-event platform is unnecessary. A user/course summary is a query over runs/certificates.

## Dependency direction

HTTP endpoints and the React application use application use-cases. Module public contracts expose commands, queries and immutable DTOs; implementation types and mutable ORM entities remain private. A thin host/application coordinator can compose modules; modules must not depend on the host. Keep public contracts in small assemblies or explicit public namespaces; internals in separate assemblies where useful. Do not create an interface for every class.

Use architecture tests for forbidden implementation references and dependency cycles. A completion coordinator may depend on Authoring, Assessment and Enrollment public interfaces without Assessment depending back on the coordinator: the application use-case invokes the coordinator after scoring, or durable work does so. Document any deliberately shared transaction; do not hide it in an event bus.

### Concrete orchestration direction

Course publication needs Authoring structure and Assessment validation. The host/application publication use-case calls both public contracts in one transaction, then calls Authoring to publish the validated requirements version. Authoring does not depend back on Assessment merely to expose a Course entity, while Assessment may consume Authoring's lesson/ownership read contract. Completion similarly composes evidence and then calls Enrollment's completion command; Enrollment does not call back into completion evaluation.

Identity owns the account query/mutation for seat accounting. It acquires Licensing's organization guard and passes the current authoritative count/delta to Licensing's policy within the shared transaction; Licensing does not acquire a dependency on Identity internals. A named integration query may serve administrative capacity displays. These are small use-cases, not a generic workflow framework. Architecture tests must reject module implementation references and contract dependency cycles, including cycles introduced through convenience helpers.

## Data access and integration reads

One PostgreSQL database is sufficient. Use module-owned tables/migration areas (schemas or naming conventions, chosen during implementation). Only the owner mutates them. Foreign keys across owned areas are permitted when they enforce an actual invariant.

Efficient joins are allowed in **explicit read-only integration queries**: for example, a cohort dashboard joining enrollment, progress, quiz results and user display names. Place such queries behind a named query interface, list tables/read columns consumed, enforce organization and cohort permissions before returning results, and test the projection. Do not load another module's mutable entities or scatter arbitrary table access throughout handlers. Batch queries; avoid N+1 interface calls and speculative duplicate read databases.

Change owners review integration-query dependencies when changing columns. An extraction later must replace local queries with an appropriate contract/projection; that cost is deliberate, not proof that all joins should be prohibited now.

## Transactions and durable work

**Current concrete seam (2026-09-18):** Authoring depends on Identity's public `IIdentityAccess`/`IdentityWork` contract. For authoring writes it opens one PostgreSQL transaction, acquires the same organization row guard as role changes/deactivation, then rechecks current authority. Authoring uses the exposed local connection/transaction only for its own schema; owner eligibility and audit persistence remain Identity operations. Module EF entities/services are internal, and architecture tests prohibit reverse/host dependencies. This coarse guard trades write concurrency for a simple, proven boundary in the initial deployment. Revisit it from measured contention, not hypothetical scale. `Variable.Database` contains only the migration manifest/runner shared by the two implemented modules; the host composes their immutable SQL resources. See [the concrete contract](../04-api-design/course-authoring.md).

- **Activation / Learner grant:** acquire organization capacity guard, validate current actor/target and license, calculate consumption delta, change Identity state, persist audit and revoke/version affected sessions in one transaction. License loading and all consuming mutations use the same guard.
- **Enrollment:** verify active Learner entitlement, organization/course/cohort consistency and authority, serialize the one-active-run-per-user/course rule, create a fresh enrollment plus audit. Enrollment itself consumes no extra seat.
- **Graded submission:** serialize against the run/quiz allowance; deduplicate the request; check content/run policy; write immutable attempt, answer snapshots and score. Within this transaction the use-case can invoke completion evaluation if it is bounded and synchronous. Otherwise persist completion work in the same commit. Never acknowledge success then depend on an in-memory callback to record completion.
- **Completion:** coordinate concurrent evaluation for an enrollment, obtain a consistent requirement version/read, calculate only that run's evidence, call Enrollment to persist completed state and requirement/evidence snapshot, insert certificate if no user/course credential exists, and enqueue completion email. Use ordinary local transactions and uniqueness constraints; handle another run winning the certificate insert without failing valid completion.
- **Notifications:** enqueue unique intent in the producer's transaction; SMTP happens after commit. Crash/retry may resend email but cannot create another enrollment/certificate. Do not send an email containing an uncommitted invite or credential.

All required audit entries join their business transaction. A durable notification/work record may use a small PostgreSQL table with attempts, next-attempt time, lease and error classification. Existing Notification is the email work record; add a generic outbox only where non-email work actually needs it. Workers run as hosted services in the same image initially. Multiple instances require claim/lease coordination, not a new broker.

## Events and extraction

Use past-tense business events only where there is a real consumer (e.g. enrollment completed). Version persisted payloads, include organization/resource IDs and an idempotency key, minimize personal data, and do not serialize ORM graphs. Synchronous module calls remain normal.

Extract a capability only after evidence of independent scaling, availability isolation, ownership/deploy cadence or organizational need. Record the problem and expected improvement. Revisit transaction boundaries, event delivery, data ownership and read models at extraction time. Distribution still uses a versioned package referencing images. No service mesh, cross-module HTTP, distributed transaction, per-module database, or mandatory eventual consistency is introduced now.

## Open questions

Product gates for access, attempts and live content changes: [Q69](../00-product/open-questions.md#q69), [Q72](../00-product/open-questions.md#q72), [Q74](../00-product/open-questions.md#q74). Their resolution changes policy, not module ownership.

## Related documents

[ADRs](decisions.md) · [Data-model conventions](../03-data-model/README.md) · [Invariants](implementation-invariants.md)
