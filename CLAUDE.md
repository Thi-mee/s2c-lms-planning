# CLAUDE.md — Instructions for Claude

## Repository Purpose

This is a **planning and documentation repository** for a Learning Management System (LMS).

It contains product thinking, feature definitions, domain models, architecture plans, and research.

There is no application code here yet. The product is in the **brainstorming and planning phase**.

---

## Current Stage

**Phase:** Brainstorming & Planning

We are defining:

- What the LMS product is and who it serves
- Core domain concepts and workflows
- Feature requirements at a conceptual level
- Data models at a conceptual level
- Open questions that need answers before implementation

We are **not yet** implementing backend code, frontend code, database schemas, or APIs.

---

## How Claude Should Work With This Repo

### Read before writing

- Before making changes, read the relevant existing documents
- Understand what has already been decided
- Check `docs/00-product/open-questions.md` for known unknowns
- Check `docs/01-domain/glossary.md` for established terminology

### Preserve product decisions

- Documented decisions represent intentional choices
- Do not silently change, remove, or contradict existing decisions
- If a decision seems wrong, flag it as a question rather than overwriting it
- When updating a decision, note what changed and why

### Avoid premature implementation

- Do not create application source code unless explicitly asked
- Do not create database migrations, ORM models, or schema files
- Do not create API server code, route handlers, or middleware
- Do not create frontend components, pages, or stylesheets
- Keep documentation at the conceptual/planning level
- It is acceptable to include pseudocode, data shape examples, or API sketches in documentation

### Propose changes carefully

- When adding a new feature, use the template at `docs/02-features/feature-template.md`
- When adding a new entity, use the template at `docs/03-data-model/entity-template.md`
- When adding a new API, use the template at `docs/04-api-design/api-template.md`
- Place files in the correct folder based on the repository structure
- Use descriptive, kebab-case filenames (e.g., `course-enrollment.md`)

### Ask for clarification only when necessary

- If a request is ambiguous but you can make a reasonable assumption, document the assumption and proceed
- If a request contradicts an existing documented decision, ask before overwriting
- If a request requires significant architectural commitment, flag the implications
- Do not ask about minor formatting or stylistic choices

### Structure future additions

- Follow the existing folder structure
- Use existing templates for new documents
- Cross-link related documents using relative markdown links
- Keep documents focused — one feature per file, one entity per file
- Use consistent heading levels and section ordering
- Mark unfinished sections as `TBD` rather than omitting them

---

## Key Files to Know

| File | Purpose |
|------|---------|
| `README.md` | Project overview and contributor guide |
| `AGENTS.md` | Instructions for all AI agents |
| `docs/00-product/vision.md` | Product vision statement |
| `docs/00-product/open-questions.md` | Tracked unknowns and decisions needed |
| `docs/01-domain/glossary.md` | Shared terminology |
| `docs/01-domain/core-concepts.md` | Core LMS domain concepts |
| `docs/02-features/feature-template.md` | Template for new features |
| `docs/03-data-model/entity-template.md` | Template for new entities |
| `docs/04-api-design/api-template.md` | Template for new APIs |

---

## Documentation Conventions

- Use **short sections** with explicit headings
- Use **tables** for structured comparisons
- Use **bullet points** for requirements and lists
- Separate **confirmed decisions** from **open questions**
- Use consistent terminology from `docs/01-domain/glossary.md`
- Prefer clarity over brevity
- Do not repeat the same decision in multiple files — cross-link instead

---

## Source of Truth

The documentation in this repository **is the source of truth** for the LMS product.

When working on any aspect of the product:

1. Check the documentation first
2. If the documentation answers your question, follow it
3. If the documentation is silent, document your assumption and proceed
4. If the documentation conflicts with a request, flag the conflict

---

## Common Tasks

### Adding a new feature

1. Copy `docs/02-features/feature-template.md` to a new file like `docs/02-features/your-feature.md`
2. Fill in all sections. Use `TBD` for sections not yet defined.
3. Update `docs/02-features/README.md` to list the new feature
4. Cross-link to related domain concepts, entities, and screens

### Answering an open question

1. Find the question in `docs/00-product/open-questions.md`
2. Move it from "Open" to "Resolved" with the answer and rationale
3. Update any documents affected by the decision

### Refining a domain concept

1. Update `docs/01-domain/core-concepts.md` or `docs/01-domain/glossary.md`
2. Check if any feature docs reference the concept and update them if needed
3. Check if any entity docs need updates
