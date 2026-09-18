# Working on Variable LMS

Variable is the vendor; Variable LMS is the product; s2c is the first customer organization.

## Read only the context needed

1. Read [docs/README.md](docs/README.md) for authority and the task routing table.
2. Read the relevant canonical domain/feature document and its entity/API guidance.
3. For architectural changes, read the applicable [ADR](docs/06-architecture/decisions.md).
4. Check the relevant [open question](docs/00-product/open-questions.md) before implementing unresolved behavior.

The September [accepted decisions](docs/00-product/accepted-decisions.md) take precedence. Historical snapshots, research recommendations, templates, and `public/` are not requirements. Do not reopen resolved decisions because an archived document differs. Flag a real conflict and continue independent work.

## Universal invariants

Use [implementation invariants](docs/06-architecture/implementation-invariants.md) as the canonical enforcement checklist: organization isolation, explicit role delegation, atomic learner capacity, enrollment-owned history, module-owned writes, durable effects, idempotent credentials, and safe migrations. Resource grants never confer organization-level security roles. Do not copy these policies into new competing matrices.

## Scope and implementation

This is the living product/implementation repository. Implement application code, tests, and deployment artifacts only when the current task authorizes implementation. The 2026-09-17 consolidation establishes documentation, not runnable product code. A modular monolith does not justify speculative services, brokers, mandatory Redis, or Kubernetes dependencies.

## Change and validate

- Read before editing; preserve unrelated working-tree changes.
- Update canonical behavior, affected contracts/tests, and derived summaries together; record decision changes and their rationale.
- Use the glossary's terms, relative documentation links, kebab-case documentation names, and existing section templates.
- Keep unresolved product choices in the central question register with a feature gate. Engineering defaults must be identified as such.
- Validate local links/anchors, active terminology and supersession references for documentation changes. For implementation, run the applicable invariant tests, build, and slice acceptance checks in the [plan](docs/07-roadmap/implementation-plan.md).
- Report what changed, what was validated, and any remaining gates. Never mark an entire feature complete while its specified acceptance or security checks remain unverified.

Scoped architectural guidance lives in [module boundaries](docs/06-architecture/module-boundaries.md), [security and identity](docs/06-architecture/security-and-identity.md), and [release and operations](docs/06-architecture/release-and-operations.md). Add further scoped instructions only when actual implementation needs them.
