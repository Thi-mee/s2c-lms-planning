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

| # | Question | Impact | Added |
|---|----------|--------|-------|
| Q1 | Is the LMS for individuals, organizations, or both? | Affects multi-tenancy, permissions, billing | June 2026 |
| Q2 | Will learning be self-paced, cohort-based, instructor-led, or hybrid? | Affects course structure, scheduling, notifications | June 2026 |
| Q3 | Is this a general-purpose LMS or domain-specific? | Affects terminology, features, integrations | June 2026 |
| Q4 | What is the initial target scale? | Affects architecture, infrastructure | June 2026 |

### Monetization

| # | Question | Impact | Added |
|---|----------|--------|-------|
| Q5 | Will the platform support paid courses? | Affects billing, payouts, pricing features | June 2026 |
| Q6 | Will there be a subscription model? | Affects user management, access control | June 2026 |
| Q7 | Is this a hosted SaaS or self-hosted product? | Affects deployment, multi-tenancy, pricing | June 2026 |

### Features

| # | Question | Impact | Added |
|---|----------|--------|-------|
| Q8 | Will there be assessments (quizzes, assignments)? | Affects course structure, grading, data model | June 2026 |
| Q9 | Will certificates be generated? | Affects completion tracking, PDF generation | June 2026 |
| Q10 | Will organizations manage learners? | Affects roles, permissions, org hierarchy | June 2026 |
| Q11 | What content types are supported (video, text, files, quizzes, live sessions)? | Affects content model, storage, player components | June 2026 |
| Q12 | Will there be discussion forums or social features? | Affects feature scope, moderation needs | June 2026 |

### Technical

| # | Question | Impact | Added |
|---|----------|--------|-------|
| Q13 | What tech stack will be used for implementation? | Affects all engineering decisions | June 2026 |
| Q14 | Should the system support SCORM or xAPI? | Affects content model, integrations | June 2026 |
| Q15 | What is the MVP? | Affects initial build scope and timeline | June 2026 |

---

## Resolved

| # | Question | Answer | Rationale | Resolved |
|---|----------|--------|-----------|----------|
| — | — | — | — | — |

*No questions resolved yet.*

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
