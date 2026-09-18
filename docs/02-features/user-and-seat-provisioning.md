# Feature: User and learner-seat provisioning

> **Status:** Confirmed baseline; linked feature-local questions remain open\
> **Authority:** Canonical feature specification\
> **Owner:** Identity & Organization; Licensing\
> **Updated:** 2026-09-17

## Purpose and users

Administrators and Organization Managers invite and manage organization accounts. Invitees establish their own credentials. Variable licenses active Learner entitlements, not all accounts or enrollments.

## Goal and non-goals

Support invite-only onboarding, role sets, deactivation/reactivation, visible learner capacity and offline license verification. Public registration, approval queues, billing, seat-purchase workflows, bulk CSV import and directory synchronization are outside the current MVP.

## Core flows

1. An authorized actor selects an email and allowed roles. Validate organization email uniqueness and the [grant policy](../01-domain/roles-and-permissions.md). A Learner invitation retains the existing capacity precheck: if already full, explain why it cannot be sent. Staff-only invitations do not require learner capacity.
2. Create a `pending` account and one-time, expiring invitation plus durable email intent in one transaction. Pending invitations consume **zero** seats and reserve none. Resending replaces the old token; it does not create another account.
3. On acceptance, validate the token and establish credentials. Atomically recheck current license capacity and activate the account. Concurrent acceptance of the last seat allows only one consuming activation. If full, leave the account pending and explain the next action; token consumption and activation must not partially commit.
4. Authorized deactivation immediately removes access, invalidates sessions and releases a seat if consumed. Preserve learning runs, submissions and certificates. Reactivation restores account access only after any positive learner-seat delta succeeds.
5. Adding Learner to an already active staff account uses the same guard; removing Learner releases capacity. Display utilization from the same predicate used for enforcement.

## Business rules

The [core seat definition](../01-domain/core-concepts.md) is authoritative: unique active, non-deleted accounts with Learner consume one each; additional roles consume no additional learner seat. Activation, reactivation, restoration and role changes cannot bypass the organization-scoped transaction guard. Enrollment does not consume another seat. Signed license loading is the only source of the licensed limit; no profile or organization PATCH may edit it.

Roles are a set in both policy and UI. Never assume a primary role grants implicit Learner access. Ordinary user administration cannot alter privileged account security or bypass final-Administrator protection. Bootstrap and Administrator assignment use their dedicated [security operations](../06-architecture/security-and-identity.md).

## Permissions

Use the canonical [account grant/revoke and resource matrices](../01-domain/roles-and-permissions.md). Apply them to invite, acceptance-side assigned roles, resend, role changes, batch operations and self-edits. Revalidate an invitation's stored assignments against current policy; a token cannot authorize an elevated role that its issuing operation could not grant.

## Data requirements

[User](../03-data-model/user.md), [Organization](../03-data-model/organization.md), protected invitation/session records, [Notification](../03-data-model/notification.md), [Audit Log](../03-data-model/audit-log.md), [license contract](../04-api-design/license-contract.md).

## Acceptance and failure cases

- Pending Learner, inactive Learner and active staff-only account each consume zero; active Learner with four staff roles consumes one.
- Race activation against adding Learner and license replacement: never exceed valid capacity through concurrent writes.
- Reject crafted role sets, cross-organization targets, privileged email/password takeover and simultaneous removal of the final usable Administrator.
- Duplicate/resend requests create no extra account; expired/used tokens cannot activate. SMTP failure retries after commit; failure to persist required audit or email intent rolls back the invitation.
- Deactivation preserves history and revokes the next protected request. Enrollment and current resource authority remain separate checks after reactivation.

## Open questions

[Q68](../00-product/open-questions.md#q68) governs expired/suspended/reduced licenses; [Q57](../00-product/open-questions.md#q57) governs retention/erasure. These are not reasons to reopen the seat predicate or grant policy.

## Related documents

[Navigation](../05-frontend/navigation.md) · [Invariants](../06-architecture/implementation-invariants.md) · [Cohort and enrollment management](cohort-and-enrollment-management.md)
