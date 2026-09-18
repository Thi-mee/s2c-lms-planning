# Research: Architecture & Stack Evaluation

> **Status:** Historical — superseded as implementation guidance
> **Authority:** Historical research\
> **Original:** July 2026; marked historical 2026-09-17
> **Scope:** Foundational technical direction — deployment/delivery model, application architecture, backend language, and frontend approach.

> **Historical notice:** This is the pre-baseline evaluation, retained for reasoning and context. Its proposed startup migrations, Redis/RLS/SaaS assumptions, vendor naming, benchmark claims and module restrictions are not current requirements. Use the [accepted decisions](../00-product/accepted-decisions.md), [current ADRs](../06-architecture/decisions.md) and [release guidance](../06-architecture/release-and-operations.md). The original proposal states are preserved in [ADR history](history/pre-baseline-adrs.md). External observations below have not been revalidated for the current baseline; verified technical sources are listed in [references](references.md).

---

## Purpose

This brief gathers external evidence to inform the LMS's foundational technical decisions. It was commissioned to deliberately re-examine the originally-recorded direction (single Go binary with an embedded SPA — [ADR-1](../06-architecture/decisions.md)) rather than treat it as settled.

Per the [research conventions](./README.md), **observations** (what the sources say) are kept separate from **recommendations** (what we propose). Recommendations here feed the `Proposed` ADRs in [decisions.md](../06-architecture/decisions.md); they are not themselves decisions.

Four axes were evaluated:

1. [Delivery & packaging model](#1-delivery--packaging-model)
2. [Application architecture (modular monolith vs. microservices)](#2-application-architecture)
3. [Backend language](#3-backend-language)
4. [Frontend approach](#4-frontend-approach)

---

## 1. Delivery & Packaging Model

**Question:** How should a self-hosted product be packaged and updated when the *vendor* wants to control application versions but the *customer* owns the infrastructure?

### Observations — how comparable self-hosted products ship

| Product | Primary delivery artifact | Update mechanism | Enforced version "stops"? |
|---------|---------------------------|------------------|---------------------------|
| **GitLab** | Official Docker/Omnibus images **+ official Helm chart** | Monthly releases; Helm runs migrations as K8s Jobs | **Yes** — required stops at `x.2/x.5/x.8/x.11` |
| **Sentry** | `docker-compose` + `install.sh` script | Re-run installer per version; migrations run by script | **Yes** — non-skippable "hard stops" |
| **Metabase** | Standard Docker image | Pull new image tag, restart; migrations auto-run at boot | No |
| **Discourse** | Custom `discourse_docker` `launcher` | `git pull` + `./launcher rebuild app` | No (but stable/beta branch caveats) |

- Compiled/containerised delivery is the norm; the artifact is almost always **a versioned image (or image set)**, not a bare binary.
- **Database migrations on upgrade are the dominant source of pain.** GitLab and Sentry both enforce non-skippable checkpoints tied to schema migrations, so a long-neglected instance must be upgraded in multiple sequential "hops." ([GitLab upgrade paths](https://docs.gitlab.com/update/upgrade_paths/), [Sentry hard-stops write-up](https://dev.to/vineethnkrishnan/i-upgraded-our-25-year-old-self-hosted-sentry-without-losing-a-single-byte-hg5))
- **`latest` lags and misleads.** Metabase self-hosters have hit cases where the `latest` image trailed the newest release; the community guidance is to **pin explicit version tags**. ([Metabase discussion](https://discourse.metabase.com/t/docker-image-hasnt-be-upgraded-for-a-while/172815))
- **Migrations run at container boot is a common, workable pattern** (Metabase, Discourse `rebuild`). Discourse splits out *post-deployment* migrations so multi-container sites stay up during upgrades. ([discourse_docker](https://github.com/discourse/discourse_docker))
- Supporting **Compose + Helm + SaaS from one codebase is realistic** but has a cost: GitLab maintains a chart-version→app-version mapping and separate upgrade docs per path ([GitLab Helm upgrade](https://docs.gitlab.com/charts/installation/upgrade/)); Sentry ships *no* official Helm chart and leans on community charts, which fragments support ([Sentry self-hosted](https://develop.sentry.dev/self-hosted/)).

### Recommendation

Ship **vendor-published, versioned OCI images** plus deployment manifests: a **Docker Compose** manifest for simple single-node self-hosting and a **Helm chart / K8s manifests** for customers who operate Kubernetes. Run the same images in vendor-managed SaaS. Concrete practices drawn from the above:

- **Pin explicit image tags** in shipped manifests; never `latest`.
- **Run schema migrations at startup behind an advisory lock**, and keep migrations **forward-compatible** so we avoid GitLab/Sentry-style mandatory "hard stops" for as long as possible.
- **Document the upgrade path** (stop → back up via `pg_dump` → pull new tag → start → health-check) as a first-class product feature.
- Treat Compose as the **supported baseline**; offer Helm as a supported-but-secondary path so we don't fragment like Sentry.

---

## 2. Application Architecture

**Question:** Monolith, modular monolith, or microservices — and how do we keep a path open to evolve?

### Observations

- **"Monolith first" is the mainstream expert position.** Sam Newman: *"microservices should not be the default choice"* and the goal is **independent deployability**, reached by *incremental* decomposition, not a big-bang rewrite. ([InfoQ / QCon](https://www.infoq.com/news/2020/05/monolith-decomposition-newman/), [Monolith to Microservices](https://samnewman.io/books/monolith-to-microservices/)) This echoes Fowler's [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html).
- **A modular monolith is one deployable with enforced internal module boundaries** (bounded contexts, explicit interfaces) — "apartments in a building," versus a monolith's "open floor plan" or microservices' "houses across a city." ([modular-monolith overview](https://singhajit.com/modular-monolith-architecture/))
- **Boundaries must be enforced by tooling, not discipline alone** — *"a modular monolith without enforced boundaries is just a monolith with folders."* Named enforcement mechanisms include .NET internal assemblies, Java's Spring Modulith, Ruby's Packwerk, and ArchUnit-style architecture tests. ([Java Code Geeks 2025](https://www.javacodegeeks.com/2025/12/microservices-vs-modular-monoliths-in-2025-when-each-approach-wins.html))
- **Separate data ownership early.** Newman advocates a *modular monolith with multiple schemas/databases*, because separating the data tier is the hardest part of any later extraction. Cross-module joins should be avoided.
- **Prefer in-process events for cross-module async workflows** — they externalise cleanly to a message broker later. ([migration patterns](https://dev.to/sepehr/from-monolith-to-modular-monolith-to-microservices-realistic-migration-patterns-36f2))
- **Extraction triggers are evidence-based, not speculative:** independent scaling need, independent deploy cadence, team ownership at scale (commonly cited: <10 devs → monolith; 10–50 → modular monolith; 50+ → microservices become worthwhile), or compliance isolation.
- **The failure mode to avoid is the "distributed monolith"** — all the cost of microservices, none of the independence. Real-world scale validation: Shopify and GitHub run large **modular monoliths** (Rails).

### Recommendation

Build a **modular monolith** with enforced boundaries and per-module data ownership inside one Postgres database, and an in-process event mechanism for cross-module workflows. Proposed module map for the LMS domain:

`Identity & Tenancy` · `Course Authoring` · `Enrollment & Cohorts` · `Assessment` · `Discussion` · `Progress & Certification` · `Notifications` · `Licensing`

Extract a service **only on evidence**. Most likely first candidates: **Notifications** (spiky, independently scalable, fault-isolatable) or **Assessment** (load spikes during timed quizzes). See [ADR-6](../06-architecture/decisions.md).

---

## 3. Backend Language

**Question:** Go, Rust, .NET, or Java — optimising for high performance *and* low developer friction, given container delivery.

### Observations

- On **TechEmpower Round 23 (Feb 2025)**, compiled languages (C#, Go, Java, Rust) form a high-throughput tier well above interpreted languages; **gaps within that tier are modest** (a reader correction notes Rust led the next contender by ~19%, not "119%"). ([TechEmpower](https://www.techempower.com/benchmarks/), [Round 23 analysis](https://dev.to/tuananhpham/popular-backend-frameworks-performance-benchmark-1bkh))
- **ASP.NET Core Minimal APIs push ~1M req/s, edging past Go Fiber**, aided by **.NET 8 native AOT** (no JIT warm-up — good for container cold starts). .NET also gets **yearly performance gains without code changes**.
- **Go's standout is memory efficiency and concurrency simplicity** — lowest memory-per-connection in the group — and a minimalist "assemble small libraries" philosophy (the flip side: re-implementing things richer ecosystems include for free). ([JetBrains Rust vs Go](https://blog.jetbrains.com/rust/2025/06/12/rust-vs-go/))
- **Rust leads raw throughput but has the steepest learning curve** and slowest iteration — a poor fit where developer velocity matters.
- **Ecosystem maturity favours .NET/Java** for a feature-rich app; .NET's NuGet has ~380k packages and ships batteries-included framework features. ([.NET vs Node/Spring/Go 2025](https://www.beyondthesemicolon.com/net-vs-node-js-spring-boot-django-go-in-2025/))
- Benchmark analysts caution these are **framework-level, not pure-language** comparisons; the right choice weighs *team skills, ecosystem, and roadmap*, not throughput alone.

### Recommendation

Under container delivery, **Go's distinctive advantages for this product (single static binary, low memory) are largely neutralised**, while its minimalist ecosystem means more assembly for a feature-heavy LMS (auth, roles, quizzes, forums, background jobs, PDF). **.NET** offers equal-or-better throughput *and* a batteries-included ecosystem (**ASP.NET Core Identity, EF Core, built-in DI, hosted services, OpenAPI**) that directly reduces development friction here. **Recommend .NET (ASP.NET Core).** Go remains a reasonable fallback if team fluency ever points that way; Rust is rejected on developer-velocity grounds. See [ADR-7](../06-architecture/decisions.md).

---

## 4. Frontend Approach

**Question:** SPA vs. SSR framework for an auth-walled, interaction-heavy LMS — weighing DX *and* deployment footprint.

### Observations

- **The core footprint difference is static files vs. a running Node server.** A Vite-built React SPA is static assets served by any web server/CDN — **no Node runtime**. Next.js SSR/RSC generally requires a **long-running Node process** in production. ([React architecture tradeoffs](https://reacttraining.com/blog/react-architecture-spa-ssr-rsc))
- **Behind a login, SSR's main benefits evaporate** — nothing to index, so SSR "adds complexity without value" for dashboards/internal tools/auth-gated apps. ([Why I walked back from Next.js to a SPA](https://dev.to/devrayat000/why-i-walked-back-from-nextjs-and-rsc-to-a-plain-spa-and-a-separate-backend-3ibo))
- **CRA is deprecated since React 19; the React team recommends Vite for SPAs.** RSC drew a lukewarm reception in the State of React 2025 survey (complexity complaints).
- **Counterpoints:** RSC can cut client bundle size (~40%); the React team leans toward frameworks *in general* — but that guidance is SEO/first-paint motivated, which doesn't apply behind auth. Next.js *can* be statically exported, but its routing model fights the pure-SPA pattern. ([Building a SPA with Next.js](https://colinhacks.com/essays/building-a-spa-with-nextjs))
- **The pragmatic pattern:** develop decoupled with a Vite dev server + HMR proxying to the API; ship the built static bundle served by the backend — good DX *without* a production Node runtime.

### Recommendation

**React SPA (Vite)**, served as static assets by the ASP.NET Core backend (or a CDN), developed decoupled (Vite dev server proxying to the .NET API), **no SSR / no Node runtime in production**. This keeps the deployment lean for self-hosters (one fewer service) and matches an auth-walled, app-like product. See [ADR-8](../06-architecture/decisions.md).

---

## Consolidated Recommendation

| Axis | Recommendation | ADR |
|------|----------------|-----|
| Delivery | Versioned OCI images + Compose (baseline) / Helm (scale) / SaaS | [ADR-5](../06-architecture/decisions.md) |
| Architecture | Modular monolith, per-module data ownership, evolvable to services | [ADR-6](../06-architecture/decisions.md) |
| Backend | .NET (ASP.NET Core) | [ADR-7](../06-architecture/decisions.md) |
| Frontend | React SPA (Vite), no SSR, static-served | [ADR-8](../06-architecture/decisions.md) |

Cross-cutting infrastructure decisions this direction enables — multi-tenancy isolation, migrations, and authentication — are captured in [ADR-9, ADR-10, ADR-11](../06-architecture/decisions.md).

---

## Sources

**Delivery / self-hosted upgrades**
- GitLab — [upgrade paths](https://docs.gitlab.com/update/upgrade_paths/), [Helm upgrade](https://docs.gitlab.com/charts/installation/upgrade/)
- Sentry — [self-hosted docs](https://develop.sentry.dev/self-hosted/), [releases](https://develop.sentry.dev/self-hosted/releases/), [hard-stops write-up](https://dev.to/vineethnkrishnan/i-upgraded-our-25-year-old-self-hosted-sentry-without-losing-a-single-byte-hg5)
- Metabase — [image-lag discussion](https://discourse.metabase.com/t/docker-image-hasnt-be-upgraded-for-a-while/172815)
- Discourse — [discourse_docker](https://github.com/discourse/discourse_docker), [update guide](https://meta.discourse.org/t/manually-update-discourse-and-docker-image-to-latest/23325)

**Architecture**
- [Sam Newman on monolith decomposition (InfoQ)](https://www.infoq.com/news/2020/05/monolith-decomposition-newman/), [Monolith to Microservices](https://samnewman.io/books/monolith-to-microservices/)
- [Fowler — MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html)
- [Microservices vs. Modular Monoliths in 2025 (Java Code Geeks)](https://www.javacodegeeks.com/2025/12/microservices-vs-modular-monoliths-in-2025-when-each-approach-wins.html)
- [Modular monolith migration patterns (DEV)](https://dev.to/sepehr/from-monolith-to-modular-monolith-to-microservices-realistic-migration-patterns-36f2)
- [Modular monolith overview (Ajit Singh)](https://singhajit.com/modular-monolith-architecture/)

**Backend language**
- [TechEmpower benchmarks](https://www.techempower.com/benchmarks/), [Round 23 analysis (DEV)](https://dev.to/tuananhpham/popular-backend-frameworks-performance-benchmark-1bkh)
- [Rust vs Go 2025 (JetBrains)](https://blog.jetbrains.com/rust/2025/06/12/rust-vs-go/)
- [.NET vs Node/Spring/Go 2025 (Beyond The Semicolon)](https://www.beyondthesemicolon.com/net-vs-node-js-spring-boot-django-go-in-2025/)

**Frontend**
- [React architecture tradeoffs: SPA/SSR/RSC (React Training)](https://reacttraining.com/blog/react-architecture-spa-ssr-rsc)
- [Why I walked back from Next.js/RSC to a SPA (DEV)](https://dev.to/devrayat000/why-i-walked-back-from-nextjs-and-rsc-to-a-plain-spa-and-a-separate-backend-3ibo)
- [Building a SPA with Next.js (Colin McDonnell)](https://colinhacks.com/essays/building-a-spa-with-nextjs)

**Multi-tenancy / data (for ADR-9–11)**
- [Row-Level Security for tenants (Crunchy Data)](https://www.crunchydata.com/blog/row-level-security-for-tenants-in-postgres)
- [Multi-tenancy database patterns](https://www.glukhov.org/post/2025/11/multitenant-database-patterns/)
- [Postgres RLS implementation guide (Permit)](https://www.permit.io/blog/postgres-rls-implementation-guide)

---

## Related Documents

- [Architecture Decisions](../06-architecture/decisions.md)
- [Architecture Principles](../06-architecture/architecture-principles.md)
- [Open Questions](../00-product/open-questions.md)
- [References](./references.md)
