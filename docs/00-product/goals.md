# Product Goals

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document captures the high-level goals for the LMS product. Goals should guide feature prioritization and architectural decisions.

---

## Primary Goals

1. **Deployable Single-Binary Architecture** — Enable hosted organizations (starting with s2c) to set up and self-host the LMS using a single compiled binary linked to a Postgres database.
2. **Flexible Seat-Based Licensing** — Provide gatekeeping mechanisms to check learner account creation limits based on yearly licensing tiers.
3. **Structured & Extensible Learning** — Enable course creation with modules, lesson notes, and required readings with external links.
4. **Knowledge Evaluation** — Offer graded and practice quizzes to assess learning outcomes.
5. **Interactive Learning Communities** — Host course-level and module-level discussion forums to drive engagement.
6. **Progress Tracking & Credentials** — Automatically track learner progress and issue digital certificates upon course completion.

---

## Non-Goals (For MVP)

- **SCORM or xAPI Integration** — Deferred to a future phase (detailed integration strategy planned).
- **Payment Processing / Storefront** — No course selling or stripe integration for the MVP (focused on s2c internal learning).
- **Live Classroom Video** — No live lectures or video conferencing (external links to meetings are acceptable).
- **Mobile Native Apps** — The web interface will be fully responsive; native iOS/Android apps are deferred.

---

## Open Questions

- Should the platform support cohort-based learning schedules (with open/close dates for modules)?
- How much customization should we offer for generated certificates (e.g., custom HTML/CSS layouts)?

---

## Related Documents

- [Vision](./vision.md)
- [Product Principles](./product-principles.md)
- [MVP Definition](../07-roadmap/mvp.md)
