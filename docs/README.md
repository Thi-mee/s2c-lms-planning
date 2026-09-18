# Documentation authority and source map

> **Status:** Confirmed\
> **Authority:** Canonical — documentation operating model\
> **Updated:** 2026-09-18

## Purpose

Find the correct source for a task without crawling the repository. The product is **Variable LMS**, published by **Variable**, initially serving **s2c**.

## Which source wins

1. [Explicit accepted decisions](00-product/accepted-decisions.md) recorded in this consolidation.
2. **Accepted** and **Accepted with modification** [ADRs](06-architecture/decisions.md), for architecture. Only their current Decision sections govern; superseded text does not.
3. Canonical product/domain rules: [MVP scope](07-roadmap/mvp.md), [core concepts](01-domain/core-concepts.md), and [roles and permissions](01-domain/roles-and-permissions.md).
4. Feature specifications and canonical entity/API contracts, within those rules. Features own behavior; entities own representation/constraints. A genuine disagreement at this level must be reconciled, not settled by guessing which file is newer.
5. Supporting implementation plans, research, and **derived** presentations such as `public/`.
6. Historical/superseded material, retained for rationale only.

The [glossary](01-domain/glossary.md) owns terminology. The [question register](00-product/open-questions.md) owns unresolved decisions, not resolved business behavior. Recommendations in questions are not decisions. A newer date alone does not approve a proposal.

## Task routing

| Task | Read first | Then consult |
|---|---|---|
| Understand product/scope | accepted decisions; MVP | vision; target users |
| Users, invitations, roles, seats | roles and permissions; core concepts | user provisioning; User; Organization; security and identity |
| Cohorts, enrollment, progress | core concepts | Cohort; Enrollment; Progress; completion feature |
| Authoring or quizzes | course administration / quiz feature | corresponding entities; module boundaries |
| Forums | forum feature; roles and permissions | Forum Thread; Forum Post |
| Licensing | license contract; ADR-3 and ADR-12 | provisioning; Organization |
| Architecture, persistence, background work | ADR log; module boundaries | implementation invariants; security and identity |
| Delivery, storage, recovery, configuration | release and operations | ADR-2, ADR-5, ADR-10; implementation plan |
| API or UI slice | owning feature; roles and permissions | API README; navigation; templates |
| Start implementation | readiness; implementation plan | relevant slice sources, not the full archive |
| Run/build/test current code | [local development](06-architecture/local-development.md) | [identity HTTP contract](04-api-design/identity-foundation.md); owning module/tests |

Section indexes provide clickable routes: [features](02-features/README.md), [entities](03-data-model/README.md), [APIs](04-api-design/README.md), [frontend](05-frontend/README.md), [architecture](06-architecture/README.md), [roadmap](07-roadmap/README.md).

## Metadata and synchronization

Use `Authority: Canonical`, `Supporting`, `Derived`, `Historical`, or `Template`. Canonical documents may contain explicitly gated sections; an `Open Question` reference never becomes an approved rule by appearing in a confirmed document. Use `Status: Proposed` for a whole unaccepted design. Feature-local gates do not invalidate the settled portions of a baseline.

Change the owning rule first, then affected entity/contracts/tests, then summaries in the same review. Keep one permission matrix. Preserve rationale with a brief change note and supersession link; do not retain contradictory guidance as an unlabeled active section. Derived pages list their canonical sources and review date.

## History and this consolidation

The [historical snapshots](08-research/history/README.md) preserve the former ADRs, question register, and user/course-based entity sketch from the pre-consolidation working tree. They include material not previously committed. The old architecture evaluation is historical research. The [readiness report](07-roadmap/readiness.md) records reconciliation and remaining gates.

## Open questions

No unresolved precedence policy. Product and release questions live in the [register](00-product/open-questions.md).

## Related documents

[Agent entrypoint](../AGENTS.md) · [Accepted decisions](00-product/accepted-decisions.md)
