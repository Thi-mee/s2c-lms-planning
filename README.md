# S2C Learning Management System — Planning Repository

> **Status:** Brainstorming & Planning Phase  
> **Last Updated:** June 2026  
> **Purpose:** Product documentation, feature definition, and system design

---

## What This Repository Is

This is the **planning and documentation repository** for a new Learning Management System (LMS).

It contains product thinking, feature definitions, domain models, architecture decisions, and research — all in structured markdown.

**This repository does not contain application code.** It is the foundation that will guide future implementation.

---

## Current Phase

The LMS product is currently in the **brainstorming and planning phase**.

We are:

- Defining what we are building and for whom
- Exploring LMS domain concepts and workflows
- Documenting features at a conceptual level
- Identifying open questions and unknowns
- Preparing the groundwork for structured implementation phases

We are **not yet** building the application, writing backend/frontend code, or finalizing database schemas.

---

## Repository Structure

```text
/
├── README.md               # This file — project overview
├── CLAUDE.md               # Instructions for Claude AI agents
├── AGENTS.md               # Instructions for all AI agents
├── docs/
│   ├── 00-product/         # Vision, goals, users, principles
│   ├── 01-domain/          # Glossary, roles, user journeys, core concepts
│   ├── 02-features/        # Feature specifications (uses template)
│   ├── 03-data-model/      # Conceptual entity definitions (uses template)
│   ├── 04-api-design/      # API planning documents (uses template)
│   ├── 05-frontend/        # Screen definitions and navigation
│   ├── 06-architecture/    # Architecture decisions and principles
│   ├── 07-roadmap/         # MVP definition and phased rollout
│   └── 08-research/        # Industry research and references
```

Each folder contains a `README.md` explaining its purpose and contents.

---

## How Documentation Is Organized

| Folder | Purpose | When to Use |
|--------|---------|-------------|
| `00-product` | High-level product direction | Defining what we're building and why |
| `01-domain` | Domain language and concepts | Establishing shared vocabulary |
| `02-features` | Individual feature specs | Detailing a specific capability |
| `03-data-model` | Conceptual data entities | Thinking about what data exists |
| `04-api-design` | API surface planning | Defining how systems will communicate |
| `05-frontend` | Screen and navigation planning | Designing user-facing interfaces |
| `06-architecture` | Technical architecture | Recording architectural decisions |
| `07-roadmap` | Build order and phases | Planning what to build when |
| `08-research` | Industry research and notes | Collecting external insights |

---

## How to Contribute

### Adding a new feature

1. Copy `docs/02-features/feature-template.md`
2. Create a new file in `docs/02-features/` with a descriptive name (e.g., `course-enrollment.md`)
3. Fill in the template sections — leave sections as `TBD` if not yet defined
4. Link related documents where applicable

### Updating existing documentation

1. Edit the relevant file directly
2. Preserve existing decisions unless explicitly revisiting them
3. Mark changed decisions with a note about why they changed
4. Update the `Status` field if present

### Adding research

1. Add notes to `docs/08-research/` or create a new file for a specific topic
2. Cite sources where possible
3. Separate observations from recommendations

---

## For AI Agents

If you are an AI agent working with this repository:

1. **Read first:** Start with `AGENTS.md` (or `CLAUDE.md` if you are Claude)
2. **Understand the phase:** We are in planning, not implementation
3. **Respect decisions:** Documented decisions are intentional
4. **Use templates:** Follow the templates in `02-features`, `03-data-model`, and `04-api-design`
5. **Track questions:** Add unknowns to `docs/00-product/open-questions.md`

---

## What Should NOT Be Added Yet

- Backend application code
- Frontend application code
- Database migrations or ORM models
- CI/CD pipelines
- Docker or deployment configurations
- Package management files (package.json, requirements.txt, etc.)

These will be added when the product direction is mature and implementation begins.

---

## License

TBD — to be determined before implementation phase.
