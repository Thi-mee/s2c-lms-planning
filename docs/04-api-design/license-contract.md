# LMS-facing license contract

> **Status:** Confirmed contract baseline; wire profile is an engineering selection to verify with issuer test vectors\
> **Authority:** Canonical — elaborates D6 and ADR-3/ADR-12\
> **Updated:** 2026-09-17

## Purpose and boundary

Variable's separate issuer produces a signed license document; the customer installation validates it locally and enforces the recognized entitlements. Only this inbound contract belongs to the LMS. Billing, checkout, pricing tiers, issuer private keys and cross-customer operations do not. No online validation, usage telemetry, automatic upgrade or vendor access is required.

## Version 1 envelope

**Engineering wire baseline:** JWS compact serialization, protected `alg = ES256`, `kid` referencing a bundled/trusted public key, and `typ = variable-lms-license`. Sign the exact encoded header/payload bytes and verify using a maintained JOSE implementation; do not reserialize JSON to recreate signed bytes. ES256 is ECDSA P-256 with SHA-256 under the JWS signature encoding. These formats are defined by [RFC 7515](https://www.rfc-editor.org/rfc/rfc7515.html) and [RFC 7518](https://www.rfc-editor.org/rfc/rfc7518.html).

Illustrative decoded payload, not a usable license:

```json
{
  "schema_version": 1,
  "product": "variable-lms",
  "license_id": "lic-example-001",
  "organization_id": "org-example-001",
  "revision": 1,
  "issued_at": "2026-09-17T00:00:00Z",
  "not_before": "2026-09-17T00:00:00Z",
  "expires_at": "2027-09-17T00:00:00Z",
  "entitlements": { "max_active_learners": 500 },
  "required_entitlements": ["max_active_learners"],
  "extensions": {}
}
```

| Field | Contract |
|---|---|
| schema_version | Required supported positive integer. Version 1 interpretation cannot be silently changed. |
| product, organization_id | Must match this product and bootstrapped organization; neither display name nor caller-selected tenant is a substitute. |
| license_id | Opaque issuer identity for audit and support; not a secret or a session credential. |
| revision | Positive issuer sequence monotonically increasing per organization, including replacement license IDs. Persist highest accepted revision; allow exact same-document reload idempotently, reject lower/reused-but-different revision. |
| issued_at, not_before, expires_at | Required UTC instants; explicit validity interval with not_before < expires_at. No undeclared perpetual/grace interpretation. Product treatment outside interval is Q68. |
| entitlements.max_active_learners | Required nonnegative integer within implementation's documented supported range. No float, negative, coercion, overflow, default infinity or generic seat_count. Zero permits no active Learner accounts. |
| required_entitlements | Names whose semantics this installation must understand before accepting the document; unknown required names cause a compatibility error. |
| extensions | Optional additional metadata. Unknown optional metadata is ignored for behavior and preserved as signed bytes; it cannot activate an unknown capability. |

The `entitlements` object may later contain named capacity/seat/feature entitlements without replacing the whole envelope. A compatible new optional entitlement is ignored by older software; a mandatory entitlement or incompatible interpretation must declare a requirement/new schema and may require an LMS upgrade. Version 1 implements **only learner capacity**. There is no expression language, general rules evaluator, product-tier inference or billing platform.

## Verification and installation

1. Bound file/header/payload size and reject malformed JSON, duplicate property names and unsupported encoding/header parameters.
2. Require the allowlisted algorithm and known trusted key. Never accept `none`, shared-secret substitution, or a key/URL supplied by the document itself. Reject unsupported JWS critical parameters.
3. Verify signature, product/organization binding, schema/types, required entitlements and validity interval. Use UTC from the installation clock and report clock errors; offline software cannot guarantee a hostile operator has not changed their clock or database.
4. Under the same organization lock used for consuming account mutations, check revision and replacement policy, then persist verified signed bytes/hash, derived entitlements, revision and audit atomically. Every consuming mutation reads that verified state and current validity, not a separately editable organization field or stale cache.
5. Reject an invalid replacement without overwriting the last verified document. The prior document's own expiry still applies; rejection does not renew it. Restrict loading to the Administrator/operator licensing operation, not ordinary user/profile endpoints. Report reason codes without leaking keys/tokens.

No license permits bootstrap to grant unlimited learner capacity: an installation without a valid entitlement can establish its staff-only Administrator and configure the license, but cannot activate Learner consumption. Existing-account behavior after expiry/suspension/downsize is [Q68](../00-product/open-questions.md#q68). Do not implement those workflows until that policy is resolved; do not silently evict learners or roll back to a more permissive old document.

## Capacity protocol

A licensed learner seat is one unique active, non-deleted account with the Learner entitlement. Invitation and enrollment do not reserve additional capacity. Inside one database transaction acquire the organization guard, recheck actor and target, compute before/after consumption delta and current usage, validate the applicable limit for positive deltas, mutate state and write required audit. Deactivation/removal releases capacity; there is no counter update that may diverge from accounts.

If an optimized counter is eventually justified, make it transactionally consistent with the same predicate and reconcile it; the authoritative predicate does not change. License replacement must use the same lock to avoid races with activation. [Provisioning](../02-features/user-and-seat-provisioning.md) covers all consuming paths.

## Keys, upgrades and recovery

Variable retains private signing keys in the separate issuer. The LMS ships an allowlisted public-key set with stable key IDs. Rotate by first delivering overlapping public trust, then issuing with the new key; retire a key only with a documented compatibility policy. No automatic network key retrieval or embedded issuer secret. Offline installations cannot receive instantaneous vendor-side revocation; a signed replacement or customer-applied trust/software update is required.

Release notes declare supported license schemas/algorithms/key IDs and upgrade requirements. Preserve the accepted license, revision and required key material in backups. Restoring older state can also restore an older revision: operator recovery must reconcile the current applicable license before reopening consuming operations. This contract is not a claim of tamper resistance against the infrastructure owner.

## Required interoperability and failure tests

Before shipping Licensing, issuer and LMS must share valid/invalid signature fixtures (test keys only), wrong-org/product, unknown-key/algorithm/schema/required-entitlement, unknown-optional-field, boundary-time, tampering, overflow, duplicate-key and revision-replay cases. Integration tests race last-seat activation/reactivation/Learner grants and license replacement against real PostgreSQL. No production private keys or usable customer licenses enter the repository.

## Open questions and related documents

[Q68](../00-product/open-questions.md#q68): lifecycle policy. Concrete JOSE library, file delivery path, payload size bounds and fixture tooling are engineering details. This wire profile may be revised with an explicit compatibility note before the first release; afterward maintain versioned support.

[Accepted D6](../00-product/accepted-decisions.md) · [Organization](../03-data-model/organization.md) · [Release operations](../06-architecture/release-and-operations.md)
