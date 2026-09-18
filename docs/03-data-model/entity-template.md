# Entity: [name]

> **Status:** Template — replace with Draft, In Review or Confirmed when used\
> **Authority:** Template, not a requirement\
> **Owner:** [module / responsible role]\
> **Updated:** [date]

## Purpose and owner

State the domain concept and module owning writes. Avoid adding a product entity for an implementation detail without need.

## Fields and relationships

List fields, types, nullability and constraints. Owned rows need organization_id; learning evidence needs enrollment_id and same-course/run integrity. Link the shared data-model conventions rather than repeating them.

## Lifecycle and invariants

Specify allowed transitions, uniqueness, tenant-consistent references, history/snapshot semantics and deletion/retention policy. Describe intended database constraints separately from application checks.

## Permissions and audit

Link the canonical permission action/resource. Identify required audit and transactional work; do not invent entity-wide CRUD grants.

## Open questions

Link central Q IDs, or state none.

## Related documents

Link the owning feature, related entities and module boundary.
