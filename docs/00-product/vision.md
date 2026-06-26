# Product Vision

> **Status:** Draft  
> **Last Updated:** June 2026

---

## What We Are Building

We are building a **Learning Management System (LMS)** designed to be **self-hosted (white-labeled)** by organizations, starting with our own organization (**s2c**). It enables organizations to create, deliver, and manage educational courses, modules, and lessons, while tracking learner progress, hosting discussions, and evaluating performance.

While the app defaults to single-tenant self-hosting, it uses an environment-based configuration design so the same codebase can be deployed in managed SaaS mode if required.

---

## Why We Are Building It

Existing LMS products are often over-engineered, expensive, or hard to deploy in a private, data-sovereign manner. By building a high-performance Go + React platform that compiles into a single binary, we allow organizations to manage their own data and infrastructure with absolute ease. 

For monetization, we will control access using a yearly support fee combined with a seat-based license keyed to the number of active students created on the platform.

---

## Vision Statement

Provide a high-performance, single-binary, extensible learning management system that empowers organizations to run their own education infrastructure, protect their data, and deliver modern e-learning without deployment overhead.

---

## What Success Looks Like

- **Self-Hosting Ease:** An organization can launch the LMS using a single compiled binary linked to a Postgres database.
- **Licensing Control:** Organizations manage their active learner seat limits based on their yearly subscription tier.
- **User Engagement:** Learners can consume content (Rich Text, Required Readings, and Lesson Notes), ask questions in Course and Module Forums, and take practice or graded quizzes.
- **Tracking & Extensibility:** Instructors and admins can track learner progress. Content formats are structured for future domain expansions (e.g., video streaming or SCORM/xAPI integrations in later phases).
- **Scale Confidence:** The application smoothly supports 1,000 to 10,000 active learners on baseline hardware, with an architecture designed to scale to 100,000.

---

## Open Questions

- What is the specific VM image format (AMI, GCP image, etc.) we should prioritize first for simplified cloud deployments?
- What are the precise seat tier thresholds for the yearly license (e.g., 500 seats, 1,000 seats, 5,000 seats)?
- Should cohort-level analytics (e.g., cohort completion rates) be exposed to the Learning Coordinator, or just to Organization Managers?

---

## Related Documents

- [Goals](./goals.md)
- [Target Users](./target-users.md)
- [Product Principles](./product-principles.md)
- [Open Questions](./open-questions.md)
