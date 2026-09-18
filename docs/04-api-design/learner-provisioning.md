# Learner licensing and provisioning HTTP contract

> **Status:** Implemented slice 3 contract; license lifecycle extensions remain gated by Q68  
> **Authority:** Supporting HTTP contract; provisioning feature, role policy and license contract remain canonical  
> **Updated:** 2026-09-18

## Scope and transaction boundary

This contract covers locally verified learner capacity, invite-only account creation, acceptance, learner-role changes, deactivation/reactivation/restoration and invitation email delivery. The authenticated session supplies the organization. No request may select another organization, edit a seat limit, mint a license or reserve capacity.

Identity acquires the organization row lock before every positive learner-capacity mutation. Licensing reads the current verified document in that same PostgreSQL transaction. License replacement takes the same lock. Identity owns the account and audit writes; Notifications inserts protected delivery work into the invitation transaction and sends SMTP only after commit.

## Licensing endpoints

### `GET /api/licensing/status`

Administrator or Organization Manager. Returns `status`, license identity/revision, validity timestamps, `maxActiveLearners`, `activeLearners` and `remaining`. Utilization is calculated from active, non-deleted accounts whose role set contains `Learner`; pending, deactivated and staff-only accounts count zero.

### `POST /api/licensing/license`

Administrator only, CSRF protected. Body: `{ "compactJws": "…" }`, bounded to the host request limit and the license verifier's 32 KiB limit. The implementation enforces the signed [version-1 contract](license-contract.md): compact ES256, exact protected type, allowlisted `kid`, strict bounded JSON without duplicate fields, product/organization binding, supported required entitlement, current validity and monotonic revision.

An exact signed-document retry is idempotent. A reused/lower revision is `license_revision_replayed`. Invalid replacement never changes current state. While Q68 remains open, a replacement below current utilization is rejected as `license_downsize_not_supported`; existing learners are not silently deactivated. Missing, future or expired licenses reject only positive consuming mutations (`license_missing`, `license_not_yet_valid`, `license_expired`).

## Invitation endpoints

### `POST /api/provisioning/invitations`

Administrator or Organization Manager, CSRF and security-rate-limit protected. Body:

```json
{
  "requestId": "UUID",
  "name": "Learner name",
  "email": "learner@example.test",
  "roles": ["Learner", "CourseAuthor"]
}
```

Roles are explicit and rechecked through the canonical delegation policy. Invitation cannot grant Administrator; a Manager cannot grant Manager. A Learner invite performs a current capacity precheck but reserves no seat. The transaction creates or updates one `pending` account, rotates the one-time token, appends audit and inserts one protected email intent. It returns `userId`, `status = pending` and `expiresAt`, never the token.

`requestId` plus a canonical payload hash implements lost-response retry: the same pair returns the existing result without rotating the token or duplicating work; reuse with another payload returns `request_id_conflict`. A new request for the same pending email is a resend: it keeps the account, rotates the token/version, and queues a new deduplicated intent.

### `POST /api/invitations/accept`

Anonymous, CSRF and identity-rate-limit protected. Body: `{ "token": "…", "password": "…" }`. Token material is stored only as a SHA-256 comparison value in Identity and as a Data Protection ciphertext in Notifications. Acceptance locks the organization and invitation, rejects expired/used/replaced tokens, revalidates the issuer's current authority, rechecks learner capacity, establishes the password and activates the account atomically. A capacity failure leaves both account and token pending/current.

## Account administration

- `GET /api/administration/users?search=` returns up to 50 organization accounts with status, role set and pending invitation expiry.
- `POST /api/administration/users/{id}/learner-role` uses `{ granted, reason, currentPassword? }`; positive grants use the capacity guard, revocation releases capacity, and both invalidate sessions.
- `POST /api/administration/users/{id}/deactivate` immediately revokes sessions and releases any learner seat.
- `POST /api/administration/users/{id}/reactivate` restores a deactivated account only after a positive learner delta succeeds.
- `POST /api/administration/users/{id}/restore` is the capacity-safe restoration path for retained soft-deleted records. Actual retention/deletion policy remains Q57-gated.

All account commands recheck current actor/target state inside the transaction and append audit before commit. Resource grants remain separate and cannot create organization account roles.

## Durable invitation delivery

`notifications.email_intents` has an organization-scoped deduplication key, protected payload, status, attempts, due time, lease and bounded error code. Workers claim with `FOR UPDATE SKIP LOCKED`, send outside the transaction, and retry transient SMTP failures with bounded exponential delay. Before sending they compare the protected token with the current unused/unexpired invitation so a stale resend is terminally suppressed. Provider acceptance marks sent; exactly-once email delivery is not claimed.

SMTP supports unauthenticated local development and customer `StartTls`, with optional username plus password file. Token values and SMTP credentials are never logged.

## Related documents

[Provisioning feature](../02-features/user-and-seat-provisioning.md) · [License wire contract](license-contract.md) · [Notification entity](../03-data-model/notification.md) · [Security](../06-architecture/security-and-identity.md) · [Q57/Q68](../00-product/open-questions.md)
