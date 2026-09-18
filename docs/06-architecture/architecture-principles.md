# Architecture principles

> **Status:** Confirmed baseline\
> **Authority:** Canonical — supporting guidance for the ADRs\
> **Updated:** 2026-09-17

## Purpose

Keep Variable LMS simple to understand, secure to operate and practical to evolve.

1. Organize a modular monolith around business ownership. Private implementation and explicit interfaces matter more than a named architecture fashion.
2. Use local transactions for local invariants. Do not introduce eventual consistency or distributed infrastructure without a concrete requirement.
3. Permit efficient, owned integration reads; prevent arbitrary cross-module writes. A join is not a microservice boundary violation in a monolith.
4. Separate distribution from application topology. Customer-controlled OCI deployment works for one application or later extracted services.
5. Enforce authorization, organization integrity, capacity and history preservation with constraints and tests wherever possible.
6. Persist work whose loss would violate an acknowledged outcome. In-memory events alone do not provide delivery guarantees.
7. Measure real workloads before adding caches, Redis, brokers or extracted services. NativeAOT is not assumed.
8. Keep upgrades, backups, restore and observability part of the product's engineering definition of done.
9. Preserve accepted product rules. Put feature-local uncertainty in the question register and keep unrelated work moving.

## Open questions

See [current questions](../00-product/open-questions.md). No named-pattern or language selection remains open.

## Related documents

[ADRs](decisions.md) · [Module boundaries](module-boundaries.md) · [Invariants](implementation-invariants.md) · [Product principles](../00-product/product-principles.md)
