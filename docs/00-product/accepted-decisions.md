# Accepted implementation-baseline decisions

> **Status:** Confirmed\
> **Authority:** Canonical — explicit product-owner decisions, highest repository precedence\
> **Decided:** 2026-09-17\
> **Provenance:** Product owner's consolidation instruction, following the repository-readiness audit

## Purpose

Record the newly accepted decisions without promoting every earlier proposal. Detailed rules live in the linked canonical specifications; this document is the durable decision record.

## D1 — Product and repository identity

**Variable** is the company/vendor. **Variable LMS** is its product, initially serving **s2c's internal training**. Keep specifications and future LMS implementation in this repository. Vendor license issuance/billing/control-plane software is a separate system; only the LMS-facing contract belongs here.

## D2 — Implementation architecture

.NET **10 LTS**, ASP.NET Core, a modular-monolith backend, React + Vite web application, PostgreSQL primary relational persistence, optional Redis only when justified, and OCI container delivery. This supersedes Go / single-binary delivery. It does **not** automatically accept every old Proposed ADR. The [ADR log](../06-architecture/decisions.md) records their individual disposition, including engineering modifications made in this pass.

## D3 — Modular boundaries

Use business-capability ownership, public module interfaces, private implementation, disciplined dependencies, module-owned writes/migrations, and deliberate integration contracts. Efficient relational joins/read models are allowed. Ordinary local transactions are preferred when they preserve an invariant. Events need a real business or reliability purpose.

No initial microservices, network calls between local modules, service mesh, speculative broker, distributed transactions, database-per-module mandate, or Kubernetes-specific application abstractions. Extraction requires demonstrated operational, scaling, ownership, reliability, or organizational value. See [module boundaries](../06-architecture/module-boundaries.md).

## D4 — Distribution independent of topology

Variable releases immutable/versioned OCI images and versioned deployment artifacts. Customers pull artifacts into infrastructure they own and choose upgrade timing. No interactive vendor server access or automatic GitOps synchronization is required. Versioned Compose packages support small deployments; Helm is the intended packaging approach for supported Kubernetes deployment, without making it mandatory for the first release.

The same released application images can be configured for on-premises, private cloud, assisted operation, and a future hosted offering. Multi-tenant SaaS provisioning, billing, isolation strategy, and operations remain separate decisions. If services are later extracted, deployment packages can reference additional independently versioned images. See [release and operations](../06-architecture/release-and-operations.md).

## D5 — Explicit privilege delegation

Organization Managers may grant/revoke Learner, Course Author, Cohort Coordinator, Learning Facilitator, and authorized resource grants in their organization. They may not grant/revoke Administrator or Organization Manager, directly or indirectly. Administrators manage Organization Managers and ordinary roles. Administrator grant/removal uses an explicitly privileged operation. Protect the final usable Administrator and audit privileged changes. Never use numeric role ordering. See the authoritative [grant matrix](../01-domain/roles-and-permissions.md#account-role-grants).

## D6 — Learner-seat licensing

One unique **active account currently holding the Learner entitlement** consumes one learner seat, regardless of other roles. Pending invitations, deactivated users, and active staff-only users consume zero. Activation, reactivation, and adding Learner to an active account must atomically recheck capacity. Deactivation or removing Learner releases capacity.

Use `max_active_learners`. Keep the license protocol versioned and extensible for future commercial entitlements without building a generic rules/billing platform. Issuance/signing/billing stay with Variable's separate system. See [core concepts](../01-domain/core-concepts.md#learner-capacity) and the [license contract](../04-api-design/license-contract.md).

## D7 — Enrollment owns learning history

An **Enrollment is the Learning Run**. Lesson progress, assessment attempts, and completion evaluation belong to that enrollment. Genuine re-enrollment creates new progress, attempt allowance, and completion state; old history remains intact. A user/course summary may be derived but must never replace raw run history. No separate achievement entity is required now.

A cohort transfer, if introduced, must explicitly continue a run or create a new one. It must never inherit progress accidentally through user/lesson keys. Transfer is not an implicitly authorized MVP feature. Existing certificate issuance semantics remain separate: this decision does not authorize a new certificate for every learning run. See [core concepts](../01-domain/core-concepts.md#enrollment-and-learning-run) and [Certificate](../03-data-model/certificate.md).

## Open questions

None for D1–D7. The remaining [feature/release gates](open-questions.md) must not reopen these decisions.

## Related documents

[Source precedence](../README.md) · [ADRs](../06-architecture/decisions.md) · [Readiness](../07-roadmap/readiness.md)
