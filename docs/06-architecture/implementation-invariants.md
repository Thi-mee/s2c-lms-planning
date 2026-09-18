# Implementation invariants and enforcement

> **Status:** Confirmed baseline\
> **Authority:** Canonical — verification obligations\
> **Updated:** 2026-09-17

## Purpose

Translate product/architecture rules into a small enforceable checklist. The mechanisms below are intended implementation obligations, not code delivered in this pass.

| ID | Invariant | Planned enforcement and failure case |
|---|---|---|
| I1 | Every organization-owned resource and relationship stays in its organization. | organization_id, composite keys/FKs where practical, trusted request/job context, negative integration tests with guessed foreign-organization IDs; RLS only if separately adopted |
| I2 | Account privileges follow the explicit grant matrix; resource grants cannot elevate security authority. | Separate role/grant commands and DTO allowlists; explicit permission predicates, no role rank; test manager self-promotion, crafted full-role-set updates, privileged-account credential/status edits and cross-resource grants |
| I3 | Final usable Administrator cannot be removed accidentally. | Serialize administrative continuity checks; test two concurrent removals/deactivations, pending-admin replacement, and replayed bootstrap |
| I4 | Only unique active Learner-entitled accounts consume capacity; increasing changes cannot oversubscribe. | Organization transaction guard covering activation/reactivation/Learner grant/license load, recomputed consumption or transactionally maintained count; test two operations for one remaining place, staff-only activation at capacity, duplicate grants and rolled-back activation |
| I5 | An Enrollment owns learning activity; re-enrollment never overwrites old runs. | enrollment_id on progress/attempts; run-course and organization FKs/validation; fresh allowances; unique active-run rule; test repeat enrollment and denied use of a lesson/quiz from another course |
| I6 | Submitted learning evidence remains point-in-time history. | Immutable attempts/answers plus content/scoring snapshots; append-only reset records; no destructive cascades from authoring/deactivation; retention/erasure only through the approved policy |
| I7 | Completion uses only the run's evidence; automatic certificate issuance retains its separate user/course uniqueness. | Serialize run evaluation; record completion evidence; unique (organization,user,course) credential; duplicate completion events and concurrent completed runs produce no duplicate credential |
| I8 | Required business audit and durable work intent cannot disappear after acknowledged success. | Same-database transaction for state/audit/work; rollback on required persistence failure; restart/lease-expiry tests; SMTP outside transaction |
| I9 | Module-owned writes and deliberate integration reads remain enforceable. | Private implementations/public contracts, architecture dependency tests, named integration-query ownership, no foreign-module mutations |
| I10 | Deactivated/revoked identities cannot retain protected access. | Authoritative session/security-version validation, current resource grants and transactional permission rechecks; replay old cookie after deactivation/privilege change |
| I11 | Untrusted content cannot become executable code or expose assessment secrets. | Sanitization and URL/type allowlists, separate learner DTOs, answer-key leakage and unauthorized PDF tests |
| I12 | Releases are immutable and upgrades cannot silently mix incompatible code/schema. | Digest/version manifest checks, separate migration/runtime identities, migration ledger/lock, readiness compatibility checks; clean install, supported upgrade, failed migration and isolated restore tests |

## Verification approach

Unit-test business calculations (learner-seat delta, grading, completion). Integration-test transactions and constraints against real PostgreSQL. Use a small browser/API end-to-end suite for the walking skeleton and boundary denials. Architecture tests verify dependencies rather than mirroring implementation details. Add failure/restart and concurrent-operation tests where the invariant depends on them.

Do not invent arbitrary coverage percentages. CI gates should be tied to these invariants and the changed slice. Validate documentation links and synchronise the canonical specification when behavior changes.

## Open questions

The [question register](../00-product/open-questions.md) marks unresolved semantics. Tests must not quietly choose answers to those questions.

## Related documents

[Accepted decisions](../00-product/accepted-decisions.md) · [Roles](../01-domain/roles-and-permissions.md) · [Plan](../07-roadmap/implementation-plan.md)
