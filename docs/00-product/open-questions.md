# Open Questions

> **Status:** Living Document  
> **Last Updated:** June 2026

---

## Purpose

This document tracks unresolved questions that affect product direction, feature design, or architecture. Questions should be moved to "Resolved" once answered, with a brief rationale.

This is the **single source of truth** for open product questions. Do not scatter open questions across documents without also listing them here.

---

## Open

### Product Direction

*No open product direction questions currently.*

### Monetization

| # | Question | Impact | Added |
|---|----------|--------|-------|
| Q7 | Is this a hosted SaaS or self-hosted product? | Leaning self-hosted (white-label) but leaving hosting open as a custom/managed service. | June 2026 |

---

## Resolved

| # | Question | Answer | Rationale | Resolved |
|---|----------|--------|-----------|----------|
| Q1 | Is the LMS for individuals, organizations, or both? | Primarily for organizations (B2B / Single-Tenant Self-Hosted or multi-tenant SaaS modes). | Follows the environment-configurable single-binary architecture to let companies run their own infrastructure. | June 2026 |
| Q2 | Will learning be self-paced, cohort-based, instructor-led, or hybrid? | Hybrid. For now, focus on cohort-based learning, but design for all. | Support cohort-based learner interaction, group forums, and coordinator management for the initial courses. | June 2026 |
| Q3 | Is this a general-purpose LMS or domain-specific? | Start general-purpose, but architect for domain extensibility. | Allows wide initial usability while facilitating plugin/add-on architectures later. | June 2026 |
| Q4 | What is the initial target scale? | Design for 1,000–10,000 active learners, but architect for 100,000. | Satisfies immediate organization demands while ensuring the architecture handles growth. | June 2026 |
| Q5 | Will the platform support paid courses? | Not for MVP (internal s2c use), but paid courses will be supported in the long run. | MVP targets internal s2c workflows; monetization features can follow in later phases. | June 2026 |
| Q6 | Will there be a subscription model? | Yes, in the long run. | Supports ongoing commercialization paths for hosted organizations. | June 2026 |
| Q8 | Will there be assessments? | Yes, both practice and graded quizzes. | Essential for assessing and validating learner knowledge/progress. | June 2026 |
| Q9 | Will certificates be generated? | Yes, certificates will be issued on course completion. | Required for professional validation and completion tracking. | June 2026 |
| Q10 | Will organizations manage learners? | Yes, organization managers can manage users, track progress, and configure seats. | Supports B2B and business-unit-level administration. | June 2026 |
| Q11 | What content types are supported? | Extensible content structure. MVP will support Rich Text, Required Readings (with external links), and Lesson Notes. | Limits initial scope while maintaining modular content players. | June 2026 |
| Q12 | Will there be forums? | Yes, Course-level and Module-level forums. | Promotes learner interaction, feedback, and engagement. | June 2026 |
| Q13 | What tech stack will be used? | Go, Postgres, Redis, Object Storage, and Vite + React SPA embedded in Go. Pragmatic React use alongside native browser APIs. | Ensures single-binary deployment ease for self-hosting, high performance, and rapid UI development. | June 2026 |
| Q14 | Should the system support SCORM/xAPI? | No for MVP; document an integration strategy for a later phase. | Compliance adds excessive early-stage complexity; a clean, internal API progress tracker is sufficient. | June 2026 |
| Q15 | What is the MVP? | s2c managing courses, modules, lessons (readings, notes), users, progress, course/module forums, and quizzes. | Delivers a complete, useful e-learning platform for internal s2c needs. | June 2026 |

---

## How to Use This Document

1. **Adding a question:** Add a new row to the appropriate "Open" table with a unique number, the question, its impact, and the date.
2. **Resolving a question:** Move the row from "Open" to "Resolved" and add the answer, rationale, and resolution date.
3. **Referencing in other docs:** Use `See [Q5](./open-questions.md)` to reference a specific question from other documents.

---

## Related Documents

- [Vision](./vision.md)
- [Goals](./goals.md)
- [MVP Definition](../07-roadmap/mvp.md)
