# Architecture decision log

> **Status:** Baseline established\
> **Authority:** Canonical — architecture\
> **Reviewed:** 2026-09-17

## Purpose and authority

Each earlier ADR was reviewed independently against the [accepted decisions](../00-product/accepted-decisions.md). `Accepted with modification` is an accepted current decision; the Change note identifies the rejected or deferred portions. The [pre-baseline snapshot](../08-research/history/pre-baseline-adrs.md) preserves the complete former text and options. No historical proposal is accepted merely by association with .NET.

## Decision log

| ADR | Current decision | Disposition | Material reason |
|---|---|---|---|
| 1 | Go / embedded-binary stack | Superseded | D2 selects .NET 10 and OCI delivery |
| 2 | External runtime configuration | Accepted with modification | Typed .NET options; allow mounted secrets; no SaaS toggle promise |
| 3 | Learner-seat license enforcement | Accepted with modification | D6 defines entitlement-based counting, atomic mutations, extensible protocol |
| 4 | Exclude SCORM/xAPI from MVP | Accepted | Sound scope boundary; future adapter is a direction, not a committed implementation |
| 5 | Versioned OCI distribution and packages | Accepted with modification | Customer upgrade control; Compose baseline; Helm conditional; independent from service topology |
| 6 | Modular monolith | Accepted with modification | Module-owned writes, deliberate joins, ordinary transactions; reject blanket join ban and obligatory async calls |
| 7 | .NET 10 LTS / ASP.NET Core | Accepted with modification | Explicit owner decision; remove Go fallback, benchmark guarantees, and AOT assumption |
| 8 | React + Vite SPA | Accepted | Matches D2; simple static delivery and decoupled development |
| 9 | Organization ownership and isolation | Accepted with modification | Single-organization deployment now; shared-tenant SaaS and mandatory RLS not approved |
| 10 | Packaged migrations, explicit upgrade step | Accepted with modification | Separate DDL identity and migration phase; locking alone does not make live upgrades safe |
| 11 | Revocable same-origin cookie sessions | Accepted with modification | PostgreSQL-backed sessions initially; Redis optional; key storage is not session revocation |
| 12 | Separate vendor control plane | Accepted with modification | D1/D6; Variable owns issuance/billing; no SaaS provisioning or telemetry requirement for MVP |
| 13 | Local atomicity and durable side effects | Accepted | Engineering baseline needed for promised audit, completion, certificate and email outcomes |

No whole existing ADR remains Proposed. Specific future capabilities (shared-tenant SaaS/RLS choice, initial Helm support, optional telemetry) are explicitly unapproved/deferred below and in the [question register](../00-product/open-questions.md). Rejected clauses remain visible in the change notes, rather than rejecting otherwise sound ADRs wholesale.

<a id="adr-1-technical-stack-selection--pragmatic-react-spa"></a>
## ADR-1 — Original Go stack

**Status:** Superseded by ADR-5, ADR-7, ADR-8.\
**Original:** June 2026. **Effective supersession:** 2026-09-17.

The former Go binary with embedded SPA is not an implementation option under the accepted baseline. Preserve the original rationale in [history](../08-research/history/pre-baseline-adrs.md#adr-1-technical-stack-selection--pragmatic-react-spa). The useful preference for native browser APIs survives in ADR-8.

<a id="adr-2-environment-based-application-configuration"></a>
## ADR-2 — External runtime configuration

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Context:** The same immutable image must run in customer-owned environments without embedding installation secrets.

**Decision:** Use ASP.NET Core typed configuration validated at startup. Environment variables configure installation behavior; mounted secret files are also supported for credentials/license/key material. Document names, required values, safe defaults and restart behavior. Tenant-editable branding belongs in organization data, not environment variables. Do not put secrets in images, Git, logs, or browser bundles.

**Change:** Replace Go `Config` terminology and the claim that an environment switch alone delivers SaaS. Redis configuration exists only if a chosen capability actually uses it. Details: [release and operations](release-and-operations.md#configuration-and-environments).

<a id="adr-3-seat-based-licensing-gatekeeper"></a>
## ADR-3 — Learner-seat license enforcement

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Context:** Variable sells licensed learning capacity, while customers control their own infrastructure.

**Decision:** Verify a vendor-signed, organization-bound, versioned license locally. Enforce `max_active_learners` using [D6](../00-product/accepted-decisions.md#d6--learner-seat-licensing) and the [license contract](../04-api-design/license-contract.md). Identity mutations that increase consumption serialize against an organization capacity guard inside the same database transaction as activation/role change and audit. License replacement uses that guard too. A count followed by an unprotected insert/update is not sufficient.

**Change:** Counts are per unique active Learner-entitled account, not all users, registrations, enrollments or roles. Signing stays outside the LMS; no live vendor dependency. Expiry/suspension/downsize effects on existing access are a feature gate [Q68](../00-product/open-questions.md#q68), not an invented grace period. Offline verification deters ordinary misuse; it is not a claim of tamper-proof enforcement against a customer controlling the host.

<a id="adr-4-delayed-scormxapi-integration-strategy"></a>
## ADR-4 — Delayed SCORM/xAPI integration

**Status:** Accepted. **Reviewed:** 2026-09-17.

**Decision:** No SCORM player, xAPI integration or LRS in MVP. First-party progress uses the LMS API and enrollment-owned records. An adapter may be evaluated later against an actual requirement. No guaranteed phase, delivery duration or standards compatibility is inferred from the old research.

<a id="adr-5-container-image-delivery-model"></a>
## ADR-5 — Container-image distribution

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** Publish versioned OCI images and versioned deployment packages; use explicit release tags and record immutable digests. Never use `latest` as the release contract. Customers select and apply upgrades. Compose is the first packaging target; Helm is the intended Kubernetes path, with release support gated by [Q63](../00-product/open-questions.md#q63). Images are independent of environment and service topology. No mandatory Kubernetes, auto-sync, interactive vendor access, or automatic SaaS capability.

**Change:** Remove automatic startup migration and simultaneous Compose/Helm/SaaS commitments. Release/package compatibility, backup, upgrade and recovery requirements are normative in [release and operations](release-and-operations.md). A future service extraction adds images/contracts to packages; it does not change the product-distribution mechanism.

<a id="adr-6-modular-monolith-architecture"></a>
## ADR-6 — Modular monolith

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** One backend application with business modules, explicit public interfaces, private implementations and module-owned writes. PostgreSQL is a shared relational database. Cross-module orchestration may use a single local transaction. Reviewed integration queries/joins are permitted for read-only projections and integrity checks. See [module boundaries](module-boundaries.md).

**Rejected clauses:** Blanket bans on joins; forcing every interaction through an async event; assuming a local event can become a broker call with no consistency redesign. Neither separate databases nor a broker are required. Extraction is evidence-driven and must reconsider contracts, consistency, failure modes and ownership at that time.

<a id="adr-7-backend-language--net-aspnet-core"></a>
## ADR-7 — .NET 10 LTS and ASP.NET Core

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** Use .NET 10 LTS / ASP.NET Core with a supported, pinned servicing release. Conventional managed-runtime deployment is the baseline. EF Core/Npgsql is the initial persistence engineering default, with explicit SQL where it improves correctness or query efficiency. Application performance must be measured against the agreed workload.

**Change:** Language is now decided, not awaiting team confirmation. Reject numerical throughput guarantees and the claim that containers eliminate runtime memory differences. NativeAOT is not a baseline requirement; EF Core's current NativeAOT support is not a production default. [Official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) confirms .NET 10 LTS; [EF Core NativeAOT guidance](https://learn.microsoft.com/en-us/ef/core/performance/nativeaot-and-precompiled-queries) documents its limitations.

<a id="adr-8-frontend--react-spa-vite-no-ssr"></a>
## ADR-8 — React SPA with Vite

**Status:** Accepted. **Reviewed:** 2026-09-17.

**Decision:** Build static React/Vite assets and serve them with the ASP.NET Core host for the initial package. Develop with Vite HMR and a same-origin API proxy. No production Node server or SSR requirement. Prefer native browser features where appropriate. API authorization remains authoritative; hiding a navigation item is not access control. Login/invitation endpoints are public entrypoints, not a public course catalog. A future public verification page does not change today's frontend architecture.

<a id="adr-9-multi-tenancy-isolation-via-organization_id--postgres-rls"></a>
## ADR-9 — Organization ownership and isolation

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** The first self-hosted deployment has one organization. Every organization-owned record carries `organization_id`; cross-record references must preserve organization and parent/run consistency. Application commands and queries enforce trusted organization context and scoped permissions. Tests use synthetic second-organization records to detect leakage; this does not promise an operational multi-tenant product.

**Change:** Shared-database SaaS and mandatory RLS are **not accepted for MVP**. They remain future design choices requiring actual hosting requirements. There is no cross-tenant Administrator endpoint or general runtime `BYPASSRLS` connection. Use separate non-owner runtime and privileged migration roles now.

If RLS is later introduced, policies must cover reads/writes/jobs, default-deny absent tenant context, use transaction-local context with connection pooling, and run under a role without superuser, owner or `BYPASSRLS` privileges. RLS does not replace resource authorization or relational consistency. Table owners normally bypass policies; see [PostgreSQL RLS documentation](https://www.postgresql.org/docs/current/ddl-rowsecurity.html). Details: [security and identity](security-and-identity.md#organization-boundary).

<a id="adr-10-database-migrations--embedded-run-at-startup"></a>
## ADR-10 — Packaged migrations and explicit upgrade phase

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** Ship ordered, immutable migrations with the release, owned by modules and coordinated by one runner. The deployment package invokes an explicit migration step with DDL credentials; the web runtime receives only runtime credentials and refuses readiness for an unsupported schema. Keep a migration ledger and lock. First packaging supports a documented quiesced upgrade; rolling/zero-downtime support requires its own compatibility evidence.

**Rejected clause:** Every web process auto-migrating with elevated credentials at boot. A runner may share the application image as a separate command; it is not a new service architecture. Locking protects concurrent migration execution, not old application traffic, SQL correctness or recovery. Never edit a shipped migration. Prefer additive/expand-contract changes; do not promise unlimited version skipping or automatic down-migration. See [Microsoft migration guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) and [upgrade protocol](release-and-operations.md#upgrade-and-recovery).

<a id="adr-11-authentication--redis-backed-session-cookies"></a>
## ADR-11 — Revocable same-origin cookie sessions

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** ASP.NET Core Identity for credentials/token flows; Secure, HttpOnly, SameSite cookies with anti-forgery protection for state changes. Use opaque server-side session tickets, initially persisted in PostgreSQL via the cookie ticket-store mechanism. Validate session/user security version and active status on protected requests. Deactivation, credential/security changes and privilege changes invalidate applicable sessions; sensitive commands recheck current authority transactionally. Persist/protect data-protection keys separately from session records.

**Change:** Redis is not required just because the old stack included it. It may replace a measured session/cache bottleneck with demonstrated revocation and outage semantics. Do not use an evictable cache as the only authoritative store of licensing, jobs or learning records. Identity's supported password hasher is the engineering default; bespoke crypto is unnecessary. [Cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) and [data-protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) describe distinct responsibilities. See [security and identity](security-and-identity.md).

<a id="adr-12-vendor-control-plane-separation"></a>
## ADR-12 — Separate Variable control plane

**Status:** Accepted with modification. **Reviewed:** 2026-09-17.

**Decision:** Variable's separate vendor system owns license issuance/signing, billing and customer commercial records. The LMS verifies/enforces the signed contract, and never contains signing private keys or a billing engine. Only the [license contract](../04-api-design/license-contract.md) is specified here. The issuer can begin as a controlled vendor process; a completed billing platform is not a dependency of the walking skeleton.

**Change:** Replace vendor name s2c with Variable; retain s2c only as customer. Optional telemetry is not a launch dependency, is disabled unless explicitly configured, and cannot gate licensed operation. No compulsory phone-home, hosted-tenant provisioner or contractual telemetry payload is introduced. Software registry/release publishing belongs to Variable's release process; it need not be implemented inside the license control-plane application.

## ADR-13 — Atomic state and durable effects

**Status:** Accepted. **Date:** 2026-09-17. **Basis:** Engineering realization of accepted invariants.

**Context:** Grading, completion, certificates and security changes cannot be lost because the process restarted; external email delivery can fail independently.

**Decision:** Use local database transactions for invariant-related state and its required audit. Cross-module commands use public interfaces with an explicit shared unit of work. Persist notification/work intent in the triggering transaction. Hosted workers claim durable work with leases, retries and deduplication. In-process events are optional dispatch, not durability. Do not add a broker or distributed transaction. Certificate uniqueness and run completion are database-protected and safe under duplicate delivery.

**Consequences:** Critical audit persistence failure rolls back the sensitive change. Exporting audit to external logs may fail without undoing the committed action. SMTP is at-least-once in failure windows; do not promise exactly-once email. Detailed ownership and transaction examples are in [module boundaries](module-boundaries.md).

## Open questions

Only feature/release gates in the [register](../00-product/open-questions.md). Engineering defaults can evolve through a scoped ADR without reopening the product baseline.

## Related documents

[Principles](architecture-principles.md) · [Invariants](implementation-invariants.md) · [Sources](../08-research/references.md)
