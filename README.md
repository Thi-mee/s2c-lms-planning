# Variable LMS

> **Status:** Implementation baseline established; application implementation has not started.\
> **Last updated:** 2026-09-17

## Purpose

Variable LMS is a product of **Variable**. Its initial deployment serves **s2c's internal training**; s2c is the first customer organization, not the vendor or the product name.

This repository is the living home of product truth, architectural decisions, and future implementation, tests, deployment packages, and operations guidance. Code and specifications stay together so a behavior change can be reviewed with its rationale and tests.

## Start here

- [Documentation source map](docs/README.md) — precedence and task-specific reading routes.
- [Accepted decisions](docs/00-product/accepted-decisions.md) — the authoritative September baseline.
- [MVP](docs/07-roadmap/mvp.md) — included and deferred product behavior.
- [ADR log](docs/06-architecture/decisions.md) — each architecture proposal reviewed independently.
- [Readiness](docs/07-roadmap/readiness.md) and [implementation sequence](docs/07-roadmap/implementation-plan.md) — safe starting point and feature gates.
- [AGENTS.md](AGENTS.md) — shared contributor/agent entrypoint; vendor-specific files only point here.

## Architecture and distribution

.NET 10 LTS / ASP.NET Core modular monolith, React + Vite, PostgreSQL, and versioned OCI images. Redis is optional and requires a demonstrated need. Application topology is independent of deployment topology. Versioned Compose packages are the small-deployment baseline; Helm is the intended Kubernetes packaging path when supported. Customers choose when to apply releases. Container distribution does not imply a multi-tenant SaaS product.

## Repository shape

Existing: `docs/00-product` through `docs/08-research`, plus `public/` (a derived documentation dossier, **not** the LMS frontend).

When implementation is authorized, add `src/backend/`, `src/web/`, `tests/`, and `deploy/compose/` alongside these documents. Add other deployment packages only when supported. Do not create placeholder projects or infrastructure in this consolidation pass. The license contract lives in `docs/04-api-design/`; a separate vendor control-plane repository will own issuance and billing.

## Contribution workflow

Read the applicable canonical documents, change the owning specification and implementation in the same review, add behavior-focused validation, and refresh affected derived material. Keep historical decisions visibly superseded. Product uncertainties belong in [open questions](docs/00-product/open-questions.md), not silent defaults. Use kebab-case for documentation filenames and the existing templates when useful.

## Open questions and distribution terms

Foundational decisions are resolved. Feature and release gates remain explicitly tracked in the question register. Product source/distribution licensing terms are not selected by this technical baseline; resolve [Q78](docs/00-product/open-questions.md#q78) before external distribution. Do not infer an open-source license from the repository layout.
