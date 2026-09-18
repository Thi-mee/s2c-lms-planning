# Architecture

> **Status:** Implementation baseline established\
> **Authority:** Supporting index\
> **Updated:** 2026-09-17

## Purpose and reading routes

Use the [source map](../README.md) for precedence. Read the applicable ADR, then the relevant detail; do not treat historical research as accepted architecture.

| Document | Owns |
|---|---|
| [ADR log](decisions.md) | Accepted choices, modifications and supersessions |
| [Module boundaries](module-boundaries.md) | Data/write ownership, module calls, integration reads, transactions and durable work |
| [Security and identity](security-and-identity.md) | Organization boundary, runtime DB roles, bootstrap, revocable sessions and data protection |
| [Release and operations](release-and-operations.md) | Versioned distribution, upgrades, backup/restore, files, configuration and observability |
| [Implementation invariants](implementation-invariants.md) | Enforceable constraints and verification obligations |
| [Principles](architecture-principles.md) | Architectural judgment |

## Open questions

Feature and release gates are centralized in [open questions](../00-product/open-questions.md). Foundational .NET/modular-monolith/OCI decisions are resolved.

## Related documents

[Accepted decisions](../00-product/accepted-decisions.md) · [Implementation plan](../07-roadmap/implementation-plan.md)
