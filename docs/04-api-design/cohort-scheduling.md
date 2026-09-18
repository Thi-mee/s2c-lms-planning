# Cohort scheduling HTTP contract

> **Status:** Implemented scheduling/staffing slice; enrollment remains unimplemented\
> **Authority:** Canonical — current HTTP contract under the domain/feature rules\
> **Updated:** 2026-09-18

## Purpose and scope

Schedule a published Course, establish its first eligible Coordinator atomically, optionally assign a Facilitator and expose a staff-scoped cohort view. The [feature specification](../02-features/cohort-and-enrollment-management.md) and [single permission matrix](../01-domain/roles-and-permissions.md) govern. An account role establishes eligibility; a `cohort_staff` row establishes resource scope. Neither operation implies the other.

All endpoints require a current active session in the configured organization. Every POST requires the anti-forgery cookie and `X-CSRF-TOKEN`. Unknown JSON fields are rejected. Q69 remains unresolved, so this contract derives `upcoming`, `active` and `ended` labels but does not authorize learner access, withdrawal, transfer, late work or archive behavior.

## Endpoints

| Method/path | Input | Result/authority |
|---|---|---|
| `GET /api/cohorts?offset=0` | Offset 0–100000 | Up to 50 summaries, ordered by creation descending then ID. Administrator/Manager see organization cohorts; Coordinator/Facilitator see only cohorts where a current matching account role and staff grant are both effective. |
| `GET /api/cohorts/{id}` | Cohort ID | One summary under the same scope. Unauthorized, foreign or absent cohort returns 404. |
| `GET /api/cohorts/eligible-staff?capability=coordinator\|facilitator&search=…` | Capability and optional email substring | Up to 50 active same-organization accounts holding the matching role. Available to Administrator, Manager and Coordinator for scheduling/staff commands. |
| `POST /api/cohorts` | `{ id, courseId, title, startAt, endAt, coordinatorId, facilitatorId? }` | Create the cohort and initial staff grants atomically (200). Administrator, Manager or Coordinator chooses an eligible same-organization Coordinator. |
| `POST /api/cohorts/{id}/staff` | `{ expectedRevision, userId, capability, assigned, reason }` | Assign/revoke one eligible staff capability (200). Assigned Coordinator, Administrator or Manager; revision and final-effective-Coordinator checks apply. |

A summary is `{ id, courseId, courseTitle, title, startAt, endAt, lifecycle, revision, createdAt, staff }`. Staff entries are `{ userId, name, email, capability, effective }`. `effective` is true only while the account is active and currently holds the matching account role. Times are stored and returned as UTC instants; the browser presents them in the viewer's local time. Lifecycle uses the half-open active window `startAt <= now < endAt`; this status calculation does not answer Q69's access-policy questions.

## Validation, concurrency and retry

- Title is required and at most 200 characters. IDs must be nonempty. `endAt` may equal but cannot precede `startAt`, matching the canonical entity constraint.
- Course selection resolves through Authoring's public contract in the write transaction and accepts only a published same-organization course. Staff selection resolves through Identity's public eligibility command in that transaction.
- The client-generated cohort ID is the create request key. A retry by the same actor with the same normalized payload returns the existing cohort; reuse with different details returns 409 `request_conflict`.
- Staff changes use `expectedRevision`. Retrying an already-achieved assignment state returns current state; a competing different change returns 409 `revision_conflict`.
- The shared organization write guard serializes course/security/staff eligibility with cohort writes. Concurrent attempts to remove the two remaining Coordinators cannot both succeed. A non-ended cohort retains at least one effective Coordinator.
- Removing Cohort Coordinator role or deactivating an account is also checked through the Enrollment module's public removal guard in the Identity transaction. Facilitator role removal preserves the resource grant as ineffective history; restoring the account role makes the unchanged scope effective again.
- Required audit actions `cohort.created`, `cohort.staff_assigned` and `cohort.staff_revoked` commit with the business mutation. Audit failure rolls back the cohort/staff change.

## Persistence and module boundary

Enrollment & Cohorts owns `enrollment.cohorts`, `enrollment.cohort_staff` and migration 3. It consumes `IIdentityAccess`/`IdentityWork` and `IAuthoringAccess`; neither Identity nor Authoring references the module. Identity owns account-role writes and invokes the registered `IAccountAccessRemovalGuard` contract before capability removal. This is synchronous local composition in one PostgreSQL transaction, not an event or distributed unit of work.

Composite foreign keys preserve organization/course/user ownership. Runtime may not delete cohort history. The module's named staff projection reads user display/role columns and course title for an authorized, bounded response; it does not mutate foreign-owned tables.

## Narrow account-role addition

`POST /api/administration/users/{id}/cohort-coordinator-role` and `POST /api/administration/users/{id}/learning-facilitator-role` accept `{ granted, reason, currentPassword? }` and return 204. They use the same explicit ordinary-role grant policy, privileged-target protection, security-version/session invalidation and transactional audit as Course Author membership. They cannot modify Learner, Manager or Administrator membership. The People screen exposes these three operational roles only; learner provisioning remains slice 3.

## Validation and feature gates

[Enrollment integration tests](../../tests/Variable.IntegrationTests/EnrollmentTests.cs) cover atomic staffing, distinct dual capabilities, current scope, organization isolation, crafted payloads, role/resource separation, concurrent Coordinator protection, audit rollback, clock boundaries, runtime permissions and v2→v3 upgrade. [Browser tests](../../src/web/tests/cohorts.spec.ts) cover scheduling and dual-capability staff display in desktop/mobile Chromium.

[Q69](../00-product/open-questions.md#q69) still gates access outside the schedule, withdrawal and archive. No Enrollment row, learner seat effect, cohort-start email or learning-access rule is introduced by this slice.

## Related documents

[Cohort entity](../03-data-model/cohort.md) · [Cohort Staff](../03-data-model/cohort-staff.md) · [Local development](../06-architecture/local-development.md)
