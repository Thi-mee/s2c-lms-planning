# MVP — Minimum Viable Product

> **Status:** Not Yet Defined  
> **Last Updated:** June 2026

---

## Purpose

This document will define the Minimum Viable Product (MVP) for the LMS — the smallest set of features that delivers value to users and validates the product direction.

---

## MVP Definition

The MVP enables our organization (**s2c**) to self-host the platform, provision seat-limited learner accounts, deliver text-based courses, facilitate social forum communication, and validate learner comprehension through quizzes.

### Included in MVP

- **User & Role Management:** Learners, Learning Coordinators, Instructors, Administrators, and Organization Managers (invitation, registration, profile views).
- **Seat-based Licensing Limits:** Core checks restricting student registrations according to the local cryptographic license key.
- **Cohort & Hybrid Management:** Align learners into synchronized cohorts associated with a specific course, overseen by a designated Learning Coordinator.
- **Course Administration:** Instructors can create/edit courses, organize them into modules, and write lessons.
- **Rich Text Lessons:** Support for rich text lesson notes and required readings (containing external resource links).
- **Practice & Graded Quizzes:** Complete quiz engine supporting multiple-choice, multi-select, and true/false questions, automatic score calculation, and progress updates.
- **Course & Module Forums:** Forum boards mounted at the course and module scopes for Q&A, thread creation, and replies, supporting cohort-scoped viewing.
- **Completion Tracking & Certificates:** Track lesson and quiz completion, displaying overall course progress and automatically generating printable completion certificates.

### Excluded from MVP (Deferred to Later Phases)

- **SCORM & xAPI Packages:** No zip file uploads or native LRS (Learning Record Store) integrations. (Deferred to Phase 2; integration strategy documented).
- **Paid Courses & Payment Processing:** No payment gateways, carts, or invoicing (deferred since MVP is for internal s2c use).
- **Video Storage & Streaming:** No native video transcoding or player hosting. Videos must be embedded via external resource links.

---

## MVP Success Criteria

- **Single Binary Setup:** An s2c engineer can launch the platform on an instance via a Go binary, configure it via env variables, and connect it to Postgres and Redis in under 10 minutes.
- **Admin Control:** An Organization Manager can import/invite 100 learners and verify that the seat counter tracks active seats accurately.
- **Cohort Coordination:** A Learning Coordinator can access their cohort's progress panel, view which learners have completed specific modules, and reply to posts in the Course Forum.
- **Learning Flow:** A learner can navigate to a course module, read a required reading lesson, pass the graded quiz, and instantly download their generated PDF completion certificate.
- **Social Engagement:** A learner can post a question to the module forum, and a coordinator or instructor can view, reply, and resolve the thread.

---

## Open Questions

- What certificate template format (e.g., standard layout with customized organization logo) is sufficient for MVP?
- Should graded quiz attempts be reset manually by an instructor if a learner fails all attempts?

---

## Related Documents

- [Phases](./phases.md)
- [Product Goals](../00-product/goals.md)
- [Features](../02-features/README.md)
