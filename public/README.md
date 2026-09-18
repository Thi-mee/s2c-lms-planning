# Variable LMS product dossier

> **Status:** Reviewed 2026-09-17\
> **Authority:** Derived documentation; never overrides canonical repository specifications

## Purpose

Four hand-maintained static pages summarize Variable LMS for stakeholders. They are documentation, not the application's React frontend. [Source precedence](../docs/README.md) applies; every page names its current canonical sources and review date.

| Page | Canonical sources |
|---|---|
| [Overview](index.html) | Vision, accepted decisions, MVP |
| [Product](prd.html) | MVP, core concepts, roles, feature index |
| [Scope](scope.html) | MVP, implementation sequence, question register |
| [Architecture](architecture.html) | ADRs, module boundaries, operations, invariants |

## Maintenance and viewing

Update the owning specification first, then this summary in the same change. Keep uncertain claims as links to open gates; do not add product commitments in the dossier. The existing styles/theme assets are presentation only.

Open the files directly, or serve the **repository root** so canonical source links remain reachable:

```sh
python3 -m http.server 8080
```

Then open `http://localhost:8080/public/index.html`. There is no build step. External fonts are optional presentation resources; the documentation remains readable if unavailable.

## Open questions and related documents

[Question register](../docs/00-product/open-questions.md) · [Readiness](../docs/07-roadmap/readiness.md). No separate product decision is owned by these pages.
