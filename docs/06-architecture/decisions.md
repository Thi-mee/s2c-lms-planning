# Architecture Decisions

> **Status:** Living Document  
> **Last Updated:** June 2026

---

## Purpose

This document records significant architecture and technology decisions using a lightweight Architecture Decision Record (ADR) format. Each decision captures the context, options considered, decision made, and reasoning.

---

## Decision Log

| # | Decision | Status | Date |
|---|----------|--------|------|
| ADR-1 | Technical Stack Selection & Pragmatic React SPA | Accepted | June 2026 |
| ADR-2 | Environment-Based Application Configuration | Accepted | June 2026 |
| ADR-3 | Seat-Based Licensing Gatekeeper | Accepted | June 2026 |
| ADR-4 | Delayed SCORM/xAPI Integration Strategy | Accepted | June 2026 |

---

### ADR-1: Technical Stack Selection & Pragmatic React SPA

**Status:** Accepted

**Context:**  
We need a tech stack that supports a self-hosted (white-label) distribution model while providing a modern, rich user experience for quizzes, forums, and reading players. Minimizing deployment overhead is essential for ease of self-hosting.

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| **Option A:** Decoupled Go API + Node.js (Next.js) | Rich SSR features, excellent Dev Experience. | Customers must run/configure two separate runtime servers (Go & Node.js). |
| **Option B:** Single Go Binary + Embedded Vite/React SPA (Recommended) | **Single-file deployment.** Modern React client side components, high performance. | No Server-Side Rendering (SSR). |
| **Option C:** Go HTML Templates + HTMX | Very lightweight, single binary, fast load times. | Harder to implement complex interactive states (like quiz timers/grading) compared to React. |

**Decision:**  
We chose **Option B**. Go will act as the single backend process, using Postgres for persistence, Redis for caching/sessions, and Object Storage for asset hosting. The frontend will be a React SPA built via Vite and compiled into static assets. These assets will be embedded directly into the Go executable via `go:embed`.

Additionally, we establish a design principle: **React is a tool to simplify work, not to replace native web APIs.** We will use native browser features and web APIs (such as native `<dialog>` elements for modals, HTML5 Form validation, and native browser navigation) where optimal, rather than importing heavy client-side React libraries.

**Consequences:**  
- Deployments require running only a single binary linked to a Postgres database.
- Developers write standard client-side React code.
- No Node.js runtime is needed in production.
- SEO is limited, but acceptable since the LMS is behind an authentication wall.

---

### ADR-2: Environment-Based Application Configuration

**Status:** Accepted

**Context:**  
We must keep deployment logic separate from application logic to allow the same codebase to run in self-hosted mode (single tenant) or multi-tenant SaaS mode.

**Decision:**  
All application configuration (database URIs, ports, Redis credentials, licensing servers, storage providers, mailers) will be managed via **Environment Variables**. Code will load configuration at startup into a unified `Config` struct.

**Consequences:**  
- No hardcoded configuration values.
- System can easily switch behavior (e.g., local storage vs. AWS S3 storage) based on environment flags.
- Simplifies configuration for self-hosting setups (using a simple `.env` file).

---

### ADR-3: Seat-Based Licensing Gatekeeper

**Status:** Accepted

**Context:**  
The business model relies on a yearly license fee based on the number of active student seats created on the self-hosted platform. We need a secure method to enforce this capacity.

**Decision:**  
We will implement a cryptographic license verification service. The self-hosted LMS will check a local, signed `license.lic` file (provided by s2c) at boot and during new user registration. The license file will contain metadata regarding maximum active student seats and expiration dates. The Go backend will validate the cryptographic signature using a compiled public key.

**Consequences:**  
- Limits can be enforced offline without requiring the customer database to be exposed to s2c.
- Learner self-registration is blocked once active seat limits are reached.
- Requires an administrative portal for managers to view current seat utilization (e.g., "450 / 500 seats occupied").

---

### ADR-4: Delayed SCORM/xAPI Integration Strategy

**Status:** Accepted

**Context:**  
SCORM and xAPI (Tin Can) are standards for e-learning content tracking. Supporting them in the MVP introduces substantial complexity (handling file zip uploads, iframe sandboxing, building/hosting a Learning Record Store (LRS), parsing state models).

**Decision:**  
We will **exclude** SCORM and xAPI support from the MVP. Instead, progress tracking will be handled via a simple internal REST API. In a later phase, we will introduce SCORM/xAPI by implementing an adapter layer that translates internal progress events into xAPI statement payloads and outputs them to an external LRS, or mounts a SCORM player component that communicates with our internal database model.

**Consequences:**  
- Reduces MVP development time by several weeks.
- Protects the database schema from being prematurely polluted by e-learning standards.
- Content creators will initially write content directly inside the platform's rich text editor (Required Readings, Notes) rather than uploading zipped SCORM files.

---

## ADR Template

Use this format when recording a new decision:

### ADR-[Number]: [Title]

**Status:** Proposed | Accepted | Superseded | Deprecated

**Context:**  
What situation or requirement prompted this decision?

**Options Considered:**

| Option | Pros | Cons |
|--------|------|------|
| Option A | ... | ... |
| Option B | ... | ... |

**Decision:**  
What was decided and why?

**Consequences:**  
What are the implications of this decision?

---

## Related Documents

- [Architecture Principles](./architecture-principles.md)
- [Open Questions](../00-product/open-questions.md)
