# AGENTS.md — Instructions for AI Agents

This file provides guidance for **any AI coding or writing agent** working with this repository, including Claude, ChatGPT, Cursor, Codex, Copilot, and similar tools.

---

## Repository Overview

This is a **planning and documentation repository** for a Learning Management System (LMS).

- **Phase:** Brainstorming & Planning
- **Content:** Product documentation, feature specs, domain models, architecture decisions
- **Code:** None yet — this is documentation only

---

## Getting Oriented

### Files to read first

1. `README.md` — Project overview and structure
2. This file (`AGENTS.md`) — Agent-specific instructions
3. `docs/00-product/vision.md` — What we are building
4. `docs/00-product/open-questions.md` — What is still undecided
5. `docs/01-domain/glossary.md` — Shared terminology
6. `docs/01-domain/core-concepts.md` — Core domain concepts

### Understanding the structure

```text
docs/00-product/     → Product direction and goals
docs/01-domain/      → Domain language, roles, concepts
docs/02-features/    → Individual feature specifications
docs/03-data-model/  → Conceptual data entity definitions
docs/04-api-design/  → API surface planning
docs/05-frontend/    → Screen and navigation planning
docs/06-architecture/→ Architecture decisions and principles
docs/07-roadmap/     → MVP and phased build plan
docs/08-research/    → Industry research and references
```

You do not need to read the entire repository to work on one feature. Read the relevant section and its related documents.

---

## Adding New Feature Documents

1. Use the template at `docs/02-features/feature-template.md`
2. Create a new file in `docs/02-features/` with a descriptive kebab-case name
3. Fill in as many sections as possible; use `TBD` for unknowns
4. Update `docs/02-features/README.md` to include the new feature
5. Cross-link to related concepts in `01-domain/`, entities in `03-data-model/`, and screens in `05-frontend/`

---

## Updating Existing Documents

- **Read before writing.** Understand what is already documented.
- **Do not silently change decisions.** If you disagree with a documented decision, flag it as a question.
- **Preserve context.** Keep existing content unless it is explicitly being revised.
- **Note changes.** When updating a decision, add a brief note about what changed and why.
- **Update cross-links.** If you rename or restructure a document, update any files that link to it.

---

## Avoiding Duplication

- Check if the topic is already covered elsewhere before creating a new document
- Cross-link to existing documents instead of repeating their content
- The **glossary** (`docs/01-domain/glossary.md`) is the single source of truth for terminology
- The **open questions** file (`docs/00-product/open-questions.md`) is the single source for unresolved decisions

---

## Keeping Terminology Consistent

- Always check `docs/01-domain/glossary.md` before introducing new terms
- If you need a new term, add it to the glossary with a clear definition
- Use the exact term from the glossary — do not use synonyms
- Example: If the glossary defines "Learner", do not use "Student" interchangeably elsewhere

---

## Documenting Assumptions

- If you make an assumption to move forward, **document it explicitly**
- Use a callout or a dedicated section like:
  ```
  > **Assumption:** We assume courses have a single instructor. This may change.
  ```
- Add related questions to `docs/00-product/open-questions.md`

---

## Separating Confirmed Decisions from Open Questions

Use clear markers throughout documentation:

- **Confirmed decisions** should be stated plainly as facts
- **Open questions** should be marked with `> **Open Question:**` or listed in the document's "Open Questions" section
- **Draft content** should be marked with a `Status: Draft` field or a `> **Draft:**` callout

Do not present uncertain ideas as confirmed decisions.

---

## Avoiding Premature Implementation

**Do not create:**

- Backend application code (servers, routes, controllers)
- Frontend application code (components, pages, styles)
- Database migrations or ORM model files
- Docker, CI/CD, or deployment configurations
- Package manifests (package.json, requirements.txt, Gemfile, etc.)

**You may include in documentation:**

- Pseudocode to illustrate a workflow
- Example JSON shapes to describe API contracts
- Example data structures to describe entities
- Mermaid diagrams to visualize architecture or flows

The line is: **documentation about code is fine; actual runnable code is not.**

---

## File Naming Conventions

- Use **kebab-case** for all filenames: `course-enrollment.md`, not `CourseEnrollment.md`
- Use **descriptive names**: `progress-tracking.md`, not `feature-7.md`
- Templates are named with a `-template` suffix: `feature-template.md`
- Section README files explain the folder's purpose

---

## Document Structure Conventions

Each document should include:

1. **Title** — Clear `# Heading` at the top
2. **Status** — Draft, In Review, Confirmed, or Superseded
3. **Purpose** — Brief description of what the document covers
4. **Content** — The main body organized with clear headings
5. **Open Questions** — Any unresolved items specific to this document
6. **Related Documents** — Links to related files in the repo

---

## Quick Reference

| Task | Where |
|------|-------|
| Add a new feature | `docs/02-features/` using `feature-template.md` |
| Add a new entity | `docs/03-data-model/` using `entity-template.md` |
| Add a new API | `docs/04-api-design/` using `api-template.md` |
| Add a new screen | `docs/05-frontend/` using `screen-template.md` |
| Define a term | `docs/01-domain/glossary.md` |
| Record a decision | `docs/06-architecture/decisions.md` |
| Track an open question | `docs/00-product/open-questions.md` |
| Add research notes | `docs/08-research/` |
| Check the build plan | `docs/07-roadmap/` |
