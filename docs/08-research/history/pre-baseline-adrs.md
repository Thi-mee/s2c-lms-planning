# Architecture Decisions — Historical snapshot

> **Authority:** Historical — superseded on 2026-09-17.
> **Do not implement from this snapshot.** Preserved from the pre-consolidation working tree, including uncommitted proposals. Use the [current source map](../../README.md) and [accepted decisions](../../00-product/accepted-decisions.md). Original status labels below describe the old state only.

> **Status:** Living Document\
> **Last Updated:** June 2026

---

## Purpose

This document records significant architecture and technology decisions using a lightweight Architecture Decision Record (ADR) format. Each decision captures the context, options considered, decision made, and reasoning.

---

## Decision Log

| # | Decision | Status | Date |
|---|----------|--------|------|
| ADR-1 | Technical Stack Selection & Pragmatic React SPA | **Superseded by [ADR-5](#adr-5-container-image-delivery-model), [ADR-7](#adr-7-backend-language--net-aspnet-core), [ADR-8](#adr-8-frontend--react-spa-vite-no-ssr)** | June 2026 |
| ADR-2 | Environment-Based Application Configuration | Accepted | June 2026 |
| ADR-3 | Seat-Based Licensing Gatekeeper | Accepted | June 2026 |
| ADR-4 | Delayed SCORM/xAPI Integration Strategy | Accepted | June 2026 |
| ADR-5 | Container-Image Delivery Model | Proposed | July 2026 |
| ADR-6 | Modular Monolith Architecture | Proposed | July 2026 |
| ADR-7 | Backend Language — .NET (ASP.NET Core) | Proposed | July 2026 |
| ADR-8 | Frontend — React SPA (Vite), No SSR | Proposed | July 2026 |
| ADR-9 | Multi-Tenancy Isolation via `organization_id` + Postgres RLS | Proposed | July 2026 |
| ADR-10 | Database Migrations — Embedded, Run at Startup | Proposed | July 2026 |
| ADR-11 | Authentication — Redis-Backed Session Cookies | Proposed | July 2026 |
| ADR-12 | Vendor Control Plane Separation | Proposed | July 2026 |

---

### ADR-1: Technical Stack Selection & Pragmatic React SPA

**Status:** ⚠️ **Superseded** (July 2026) by [ADR-5](#adr-5-container-image-delivery-model) (delivery model), [ADR-7](#adr-7-backend-language--net-aspnet-core) (backend language), and [ADR-8](#adr-8-frontend--react-spa-vite-no-ssr) (frontend). The **decoupled-dev / rich React client** intent of this ADR carries forward; what changed is (a) the packaging — from a single Go binary with `go:embed` to versioned container images — and (b) the backend language — from Go to .NET. See [architecture-evaluation.md](../architecture-evaluation.md) for the reasoning.

**Context:**\
We need a tech stack that supports a self-hosted (white-label) distribution model while providing a modern, rich user experience for quizzes, forums, and reading players. Minimizing deployment overhead is essential for ease of self-hosting.

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **Option A:** Decoupled Go API + Node.js (Next.js) | Rich SSR features, excellent Dev Experience. | Customers must run/configure two separate runtime servers (Go & Node.js). |
| **Option B:** Single Go Binary + Embedded Vite/React SPA (Recommended) | **Single-file deployment.** Modern React client side components, high performance. | No Server-Side Rendering (SSR). |
| **Option C:** Go HTML Templates + HTMX | Very lightweight, single binary, fast load times. | Harder to implement complex interactive states (like quiz timers/grading) compared to React. |

**Decision:**\
We chose **Option B**. Go will act as the single backend process, using Postgres for persistence, Redis for caching/sessions, and Object Storage for asset hosting. The frontend will be a React SPA built via Vite and compiled into static assets. These assets will be embedded directly into the Go executable via `go:embed`.

Additionally, we establish a design principle: **React is a tool to simplify work, not to replace native web APIs.** We will use native browser features and web APIs (such as native `<dialog>` elements for modals, HTML5 Form validation, and native browser navigation) where optimal, rather than importing heavy client-side React libraries.

**Consequences:**\
- Deployments require running only a single binary linked to a Postgres database.
- Developers write standard client-side React code.
- No Node.js runtime is needed in production.
- SEO is limited, but acceptable since the LMS is behind an authentication wall.

---

### ADR-2: Environment-Based Application Configuration

**Status:** Accepted

**Context:**\
We must keep deployment logic separate from application logic to allow the same codebase to run in self-hosted mode (single tenant) or multi-tenant SaaS mode.

**Decision:**\
All application configuration (database URIs, ports, Redis credentials, licensing servers, storage providers, mailers) will be managed via **Environment Variables**. Code will load configuration at startup into a unified `Config` struct.

**Consequences:**\
- No hardcoded configuration values.
- System can easily switch behavior (e.g., local storage vs. AWS S3 storage) based on environment flags.
- Simplifies configuration for self-hosting setups (using a simple `.env` file).

---

### ADR-3: Seat-Based Licensing Gatekeeper

**Status:** Accepted

**Context:**\
The business model relies on a yearly license fee based on the number of active student seats created on the self-hosted platform. We need a secure method to enforce this capacity.

**Decision:**\
We will implement a cryptographic license verification service. The self-hosted LMS will check a local, signed `license.lic` file (provided by s2c) at boot and during new user registration. The license file will contain metadata regarding maximum active student seats and expiration dates. The Go backend will validate the cryptographic signature using a compiled public key.

**Consequences:**\
- Limits can be enforced offline without requiring the customer database to be exposed to s2c.
- Learner self-registration is blocked once active seat limits are reached.
- Requires an administrative portal for managers to view current seat utilization (e.g., "450 / 500 seats occupied").
- License **issuance** (minting and signing `license.lic`) happens in a separate **vendor control plane**, not the LMS — see [ADR-12](#adr-12-vendor-control-plane-separation). The bundled LMS only *verifies* and *enforces*.

---

### ADR-4: Delayed SCORM/xAPI Integration Strategy

**Status:** Accepted

**Context:**\
SCORM and xAPI (Tin Can) are standards for e-learning content tracking. Supporting them in the MVP introduces substantial complexity (handling file zip uploads, iframe sandboxing, building/hosting a Learning Record Store (LRS), parsing state models).

**Decision:**\
We will **exclude** SCORM and xAPI support from the MVP. Instead, progress tracking will be handled via a simple internal REST API. In a later phase, we will introduce SCORM/xAPI by implementing an adapter layer that translates internal progress events into xAPI statement payloads and outputs them to an external LRS, or mounts a SCORM player component that communicates with our internal database model.

**Consequences:**\
- Reduces MVP development time by several weeks.
- Protects the database schema from being prematurely polluted by e-learning standards.
- Content creators will initially write content directly inside the platform's rich text editor (Required Readings, Notes) rather than uploading zipped SCORM files.

---

### ADR-5: Container-Image Delivery Model

**Status:** Proposed (supersedes the packaging aspect of [ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa))

**Context:**\
The original plan shipped a single Go binary with the SPA embedded via `go:embed` and a local `license.lic` file ([ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa), [ADR-3](#adr-3-seat-based-licensing-gatekeeper)). On reflection, we want a delivery model that (a) lets each customer own and run the infrastructure in their own environment, (b) lets s2c keep tight control over application versions and ship clean updates, and (c) serves single-tenant self-host **and** managed SaaS from one codebase. See the evidence in [architecture-evaluation.md §1](../architecture-evaluation.md#1-delivery--packaging-model).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **A:** Single static binary (drop-in) | Simplest artifact; no container runtime needed | Ties us to a single-binary language story; awkward for multi-process concerns (cache, workers); harder to add services later |
| **B:** Vendor-published container images + Docker Compose manifest (Recommended baseline) | Customer owns infra; updates = new image tags; repeatable; works on-prem/cloud | Customer needs Docker; migration-on-upgrade discipline required |
| **C:** Container images + Helm/Kubernetes manifests | Scales; fits customers already on K8s | Higher operator skill floor; more surface to support |
| **D:** Vendor-managed SaaS only | Zero customer ops | Abandons the data-sovereignty differentiator |

**Decision:**\
s2c publishes **versioned OCI container images**. Customers deploy by applying a **Docker Compose manifest (supported baseline)** or **Helm chart / K8s manifests (supported secondary path for scale)** that reference specific image tags. The **same images run in s2c-managed SaaS**. Operational rules, drawn from how GitLab/Sentry/Metabase/Discourse do it:

- Shipped manifests **pin explicit image tags** — never `latest` (avoids the Metabase `latest`-lag trap).
- **Schema migrations run at startup behind an advisory lock** ([ADR-10](#adr-10-database-migrations--embedded-run-at-startup)); migrations are kept **forward-compatible** to avoid GitLab/Sentry-style mandatory "hard stops" for as long as possible.
- A documented upgrade path: *stop → `pg_dump` backup → pull new tag → start (auto-migrate) → health-check*.
- Licensing ([ADR-3](#adr-3-seat-based-licensing-gatekeeper)) is unchanged in principle — the signed license file is mounted into / verified inside the container.

**Consequences:**\
- The "single binary" language in [vision.md](../../00-product/vision.md) and [goals.md](../../00-product/goals.md) is now stale and must be revised (tracked as an open question).
- Customers need a container runtime (Docker/K8s) — a slightly higher floor than a bare binary, but standard for the target buyers.
- Maintaining Compose **and** Helm **and** SaaS from one codebase is a real ongoing cost; Compose is the primary supported path to contain it.
- Configuration remains environment-variable driven ([ADR-2](#adr-2-environment-based-application-configuration)) — unchanged and reinforced by this model.

---

### ADR-6: Modular Monolith Architecture

**Status:** Proposed

**Context:**\
We must choose an application architecture that is simple to build, deploy, and operate now (small team, single deployable per [ADR-5](#adr-5-container-image-delivery-model)) without foreclosing a later move to independently-deployable services if scale or team growth demands it. See [architecture-evaluation.md §2](../architecture-evaluation.md#2-application-architecture).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **A:** Big-ball-of-mud monolith | Fast to start | Degrades into unmaintainable coupling; painful to split later |
| **B:** Modular monolith (Recommended) | One deployable; enforced boundaries; cheap to operate; clean extraction path | Requires discipline + tooling to keep boundaries real |
| **C:** Microservices from day one | Independent scaling/deploy | Operational overhead, distributed transactions, "distributed monolith" risk — unjustified at our team size and scale |

**Decision:**\
Build a **modular monolith**. Concretely:

- Organize the codebase into **modules by bounded context**, not by technical layer.
- **Enforce boundaries with tooling** (in .NET: internal access modifiers / separate assemblies / ArchUnitNET-style architecture tests), so a boundary violation fails the build — not just code review.
- **Each module owns its data** (its own tables/schema within the shared Postgres database); **no cross-module joins** — modules read each other's data through interfaces.
- **Cross-module async workflows go through an in-process event mechanism**, shaped so it can later be swapped for a message broker with minimal churn.

Proposed initial modules: `Identity & Tenancy`, `Course Authoring`, `Enrollment & Cohorts`, `Assessment`, `Discussion`, `Progress & Certification`, `Notifications`, `Licensing`.

> The **Licensing** module is **verify / enforce / report** only. License *issuance*, cross-organization management, and subscription/billing live in a separate **vendor control plane**, not this codebase — see [ADR-12](#adr-12-vendor-control-plane-separation).

**Consequences:**\
- Extraction to a service is a later, **evidence-driven** step (triggers: independent scaling, independent deploy cadence, team ownership at scale, compliance isolation). Likely first candidates: **Notifications** or **Assessment**.
- We accept ongoing discipline cost to keep boundaries enforced.
- This satisfies architecture principles [1 (keep it simple)](../../06-architecture/architecture-principles.md) and [2 (separate concerns)](../../06-architecture/architecture-principles.md).

---

### ADR-7: Backend Language — .NET (ASP.NET Core)

**Status:** Proposed (supersedes the Go choice in [ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa))

**Context:**\
[ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa) chose Go primarily for its single static binary. Once delivery is container-based ([ADR-5](#adr-5-container-image-delivery-model)), that advantage is neutralized, so the language decision was reopened on the criteria **high performance + low developer friction**. See [architecture-evaluation.md §3](../architecture-evaluation.md#3-backend-language).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **Go** | Lowest memory/connection; simple concurrency; fast compile | Minimalist ecosystem → more assembly for a feature-rich LMS; binary advantage neutralized by containers |
| **.NET (ASP.NET Core)** (Recommended) | Equal-or-better throughput (~1M req/s, native AOT); batteries-included (Identity, EF Core, DI, hosted services, OpenAPI) → low friction for a feature-heavy app | Larger container images than Go; team must know C# |
| **Rust** | Highest raw throughput; memory safety | Steepest learning curve; slowest iteration — fails the "low friction" criterion |
| **Java** | Mature ecosystem; Spring Boot | Heavier runtime/ceremony; less lean than .NET AOT |

**Decision:**\
Adopt **.NET (ASP.NET Core)**. For an LMS (auth, roles, quizzes, forums, background jobs, PDF), .NET's built-in framework features directly reduce development friction while matching or beating Go on throughput. **Go remains a reasonable fallback** should team fluency ever point that way; Rust is rejected on developer-velocity grounds.

**Consequences:**\
- Auth can lean on **ASP.NET Core Identity** ([ADR-11](#adr-11-authentication--redis-backed-session-cookies)); persistence on **EF Core** (with migrations per [ADR-10](#adr-10-database-migrations--embedded-run-at-startup)).
- Container images are larger than a Go `scratch` image (acceptable under [ADR-5](#adr-5-container-image-delivery-model)); native AOT / trimming can mitigate.
- Hiring/onboarding targets the C#/.NET talent pool.

---

### ADR-8: Frontend — React SPA (Vite), No SSR

**Status:** Proposed (reaffirms and refines the React choice in [ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa))

**Context:**\
The application is entirely behind authentication and is interaction-heavy (quiz player, forums, dashboards). We must choose a frontend approach weighing developer experience **and** the impact on what customers must deploy. See [architecture-evaluation.md §4](../architecture-evaluation.md#4-frontend-approach).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **React SPA (Vite)** (Recommended) | Static assets — **no Node runtime in prod**; simplest deploy; great DX with HMR | Larger client bundle than RSC; no SSR |
| **Next.js (SSR/RSC)** | SSR/SEO, bundle trimming | Requires a **Node server** in every deployment; SSR value ≈ 0 behind a login; added complexity |
| **SvelteKit / Remix** | Modern DX | Same "adds a Node runtime" concern for SSR; smaller talent pool |

**Decision:**\
Build a **React SPA with Vite**, served as static assets by the ASP.NET Core backend (or a CDN). Develop **decoupled** (Vite dev server + HMR proxying to the .NET API); ship the built static bundle. **No SSR, no production Node runtime.** Follow [ADR-1](#adr-1-technical-stack-selection--pragmatic-react-spa)'s principle of preferring native web APIs over heavy client libraries where practical.

**Consequences:**\
- The customer's deployment has one fewer moving part (no Node process) — reinforcing [ADR-5](#adr-5-container-image-delivery-model)'s low-overhead goal.
- SEO is not available (irrelevant behind auth); no SSR-based bundle trimming (acceptable).
- Frontend and backend are developed decoupled but shipped together in the image.

---

### ADR-9: Multi-Tenancy Isolation via `organization_id` + Postgres RLS

**Status:** Proposed (resolves [Q53](pre-baseline-questions.md#q53--multi-tenancy-isolation-strategy))

**Context:**\
Even in single-tenant self-host mode, the schema must be tenancy-aware so the same codebase runs multi-tenant SaaS without a painful retrofit. This is one of the most expensive decisions to reverse. See [architecture-evaluation.md](../architecture-evaluation.md).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **Database-per-tenant** | Strong isolation | Heavy ops; poor fit for many small tenants |
| **Schema-per-tenant** | Good isolation | Migration fan-out across schemas |
| **Row-based (`organization_id`) + Postgres RLS** (Recommended) | Identical in single- and multi-tenant; simplest ops; defense-in-depth | Every query must be tenant-scoped; RLS adds query-plan considerations |

**Decision:**\
Put an **`organization_id` on every tenant-scoped table** and enforce isolation with **Postgres Row-Level Security** policies keyed to a session variable, set per-request via `SET LOCAL` **inside a transaction** (critical with pooled connections) using the Npgsql driver. The application runs as a **non-superuser role**; a separate `BYPASSRLS` connection is used only for controlled admin/cross-tenant tasks. Index `organization_id`. Layer application RBAC ([ADR-11](#adr-11-authentication--redis-backed-session-cookies) / [Q51](pre-baseline-questions.md#q51--authorization-enforcement-model-mvp-blocker)) on top for role-based rules. Use tenant-scoped unique constraints (e.g., `UNIQUE(organization_id, email)`).

**Consequences:**\
- Defense-in-depth: a forgotten `WHERE organization_id = …` cannot leak cross-tenant data.
- Single-tenant deployments simply have one `Organization` row.
- RLS is row-level only; column-level and complex attribute rules still live in the app layer.

---

### ADR-10: Database Migrations — Embedded, Run at Startup

**Status:** Proposed (resolves [Q40](pre-baseline-questions.md#q40--how-are-database-migrations-managed))

**Context:**\
A container-delivered, customer-operated product ([ADR-5](#adr-5-container-image-delivery-model)) needs upgrades to migrate the customer database automatically and safely, with no s2c access to their systems. See [architecture-evaluation.md §1](../architecture-evaluation.md#1-delivery--packaging-model).

**Decision:**\
Bundle **versioned SQL migrations inside the image** and run them **at application startup behind a Postgres advisory lock** (safe even if the customer later scales to multiple replicas). Use **EF Core Migrations** (or a dedicated tool such as DbUp/FluentMigrator) as the mechanism. Rules: migrations are **forward-only and small**, each change is a new migration (never edit a shipped one), use idempotent guards (`IF [NOT] EXISTS`), handle the "nothing to migrate" case cleanly, and keep migrations **backward-compatible** so a running old instance tolerates the new schema during rollout.

**Consequences:**\
- Delivers the "pull new tag → start → it migrates itself" upgrade experience.
- A pre-upgrade `pg_dump` backup is a documented, recommended step (ties to backup/DR, [Q58](pre-baseline-questions.md#q58--backup--disaster-recovery)).
- Advisory locking prevents concurrent-instance migration races.

---

### ADR-11: Authentication — Redis-Backed Session Cookies

**Status:** Proposed (resolves [Q50](pre-baseline-questions.md#q50--authentication--session-model-mvp-blocker))

**Context:**\
The first-party SPA ([ADR-8](#adr-8-frontend--react-spa-vite-no-ssr)) talks to a same-origin API. We need an auth model that is easy to revoke, simple, and fits the stack; Redis is already in the architecture. See [architecture-evaluation.md](../architecture-evaluation.md).

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **Redis-backed HTTP-only session cookies** (Recommended) | Server-side revocation, "log out everywhere," simple for same-origin SPA; native to ASP.NET Core cookie auth | Needs CSRF protection |
| **JWT access/refresh tokens** | "API-native", stateless | Hard to revoke; refresh-token rotation complexity |

**Decision:**\
Use **ASP.NET Core cookie authentication with HTTP-only, Secure, SameSite cookies**, backed by a **Redis distributed cache** for session/data-protection state (enabling revocation and multi-instance sessions). Hash passwords with a modern algorithm (**Argon2id** preferred; ASP.NET Identity's PBKDF2 is an acceptable default). Add **CSRF protection** (anti-forgery tokens), a defined password policy, a password-reset flow, and the **invite-acceptance tokened flow** (tokened email → set password) that the seat model ([ADR-3](#adr-3-seat-based-licensing-gatekeeper), [Q21](pre-baseline-questions.md#q21--q46--q49--learner-onboarding-self-registration-first-run-experience-course-assignment)) depends on. Authorization enforcement (middleware RBAC + service-layer resource/tenant checks) is specified separately ([Q51](pre-baseline-questions.md#q51--authorization-enforcement-model-mvp-blocker)).

**Consequences:**\
- Sessions are revocable and survive multiple app instances via Redis.
- CSRF handling becomes a standing requirement for state-changing endpoints.
- Must be specified/implemented before any protected endpoint.

---

### ADR-12: Vendor Control Plane Separation

**Status:** Proposed (complements [ADR-2](#adr-2-environment-based-application-configuration), [ADR-3](#adr-3-seat-based-licensing-gatekeeper), [ADR-5](#adr-5-container-image-delivery-model))

**Context:**\
On a self-hosted instance there is exactly **one** Organization — the customer. Yet several responsibilities involve *all* customers or require secrets that must never sit on customer-controlled infrastructure: minting and signing license files, managing the roster of customer organizations, and running subscription/billing. [ADR-3](#adr-3-seat-based-licensing-gatekeeper) already keeps license *verification* offline; this ADR names where the corresponding *issuance* and cross-tenant management live.

**Decision:**\
Split the system into **two planes**:

- **The LMS product** (the bundled application in this repo — self-hosted, or the SaaS runtime): tenant-aware (`organization_id` + RLS, [ADR-9](#adr-9-multi-tenancy-isolation-via-organization_id--postgres-rls)); **verifies** a signed `license.lic` ([ADR-3](#adr-3-seat-based-licensing-gatekeeper)); **enforces** seat limits; **reports** seat usage. It never issues licenses, manages other organizations, or processes payments.
- **The Vendor Control Plane** (a **separate s2c-operated application, NOT bundled and NOT installed at customers**): manages the roster of customer organizations, **generates and cryptographically signs** license files, owns the **subscription / renewal / billing** lifecycle, ingests seat-usage telemetry, and (per [ADR-5](#adr-5-container-image-delivery-model)) publishes the versioned container images customers deploy.

The control plane is a **distinct system**, out of scope for this planning repo **except for the contract** between the two planes.

**The LMS ↔ Control Plane contract:**

| Direction | Payload | Notes |
|-----------|---------|-------|
| Control plane → LMS | Signed `license.lic` (public-key verifiable) carrying organization identity, `max_active_seats`, `expires_at`, status | The LMS treats these as **read-only, derived state** — never edits them in-app |
| LMS → Control plane | *Optional* seat-usage / health telemetry (active seat count, version) to a configured endpoint | Mechanism TBD; must be privacy-respecting and **disableable for air-gapped installs** |

Given a valid license file, the LMS runs **fully offline** — no live dependency on the control plane.

**Consequences:**\
- `Organization` on a self-hosted instance is effectively **single-row config**: identity + display + *verified* license state — not a subscription manager. A seat increase arrives as a **new signed license file**, not an in-app edit (consistent with [Q25](pre-baseline-questions.md#q25--approval-step-for-seat-limit-increases)). See [organization.md](../../03-data-model/organization.md).
- The **Licensing** module ([ADR-6](#adr-6-modular-monolith-architecture)) is verify/enforce/report only.
- **SaaS mode** uses the same split: the control plane provisions `Organization` rows and issues entitlements; the LMS core stays billing-agnostic.
- Building the control plane is **separate work** (its own repo/app); this repo models only the contract surface. Scope tracked in [Q67](pre-baseline-questions.md#q67--vendor-control-plane-scope--boundary).

---

## ADR Template

Use this format when recording a new decision:

### ADR-[Number]: [Title]

**Status:** Proposed | Accepted | Superseded | Deprecated

**Context:**\
What situation or requirement prompted this decision?

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| Option A | ... | ... |
| Option B | ... | ... |

**Decision:**\
What was decided and why?

**Consequences:**\
What are the implications of this decision?

---

## Related Documents

- [Architecture Principles](../../06-architecture/architecture-principles.md)
- [Open Questions](pre-baseline-questions.md)
