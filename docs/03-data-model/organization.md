# Entity: Organization

> **Status:** Confirmed baseline\
> **Authority:** Canonical — entity representation\
> **Owner modules:** Identity & Organization owns identity/branding; Licensing owns verified license projection\
> **Updated:** 2026-09-17

## Purpose

Customer ownership boundary. The first installation has exactly one Organization (initially s2c). Variable is the vendor, not an automatically privileged organization in the customer database. This is not a subscription manager or SaaS tenant provisioner.

## Key fields

| Field | Requirement | Meaning |
|---|---|---|
| id | Required | Stable organization identity, bound to the signed license |
| name | Required | Customer display name |
| logo_storage_key | Optional | Private owned branding asset; not an arbitrary fetched URL |
| certificate_signature_line | Optional | Configurable display text for the fixed certificate template |
| max_active_learners | Derived/read-only | Limit from a verified supported license; never infer unlimited from absence |
| license_id / license_schema_version | Derived/read-only | Verified contract identity/version |
| license_status / license_expires_at | Derived/read-only | Verification/time/status state; exceptional behavior Q68 |
| created_at / updated_at | Required | Lifecycle timestamps |
| deleted_at | Optional | Administrative soft marker; no ordinary destructive cascade |

## Ownership and validation

Users, courses and all dependent records reference this root. Tenant-editable branding changes do not change signed organization identity. License fields are projections owned by Licensing and may live separately from the identity row physically. UI/API roles cannot edit them. License replacement verifies the document and updates its projection under the same organization guard used for learner-consuming mutations; audit the result.

Active learner consumption is derived using [core concepts](../01-domain/core-concepts.md#learner-capacity), not a count of accounts/enrollments or an editable number. A persisted counter is only permitted if maintained transactionally and reconciled against authoritative account state.

## Lifecycle and authorization

Bootstrap creates the sole organization through the controlled operator workflow. There is no ordinary in-app create/delete organization catalog for MVP. Managers/Admins may update branding and inspect capacity under the [role policy](../01-domain/roles-and-permissions.md). Decommissioning is an operator/retention workflow, not deleting customer learning history through generic CRUD.

## Open questions

[Q68](../00-product/open-questions.md#q68): expired/suspended/downsize behavior. [Q57](../00-product/open-questions.md#q57): retention/decommissioning. Vendor-side billing is outside this repository.

## Related documents

[User](user.md) · [License contract](../04-api-design/license-contract.md) · [ADR-12](../06-architecture/decisions.md#adr-12-vendor-control-plane-separation)
