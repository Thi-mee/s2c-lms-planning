# Initial Entities — Historical snapshot

> **Authority:** Historical — superseded on 2026-09-17.
> **Do not implement from this snapshot.** Preserved from the pre-consolidation working tree, including uncommitted proposals. Use the [current source map](../../README.md) and [accepted decisions](../../00-product/accepted-decisions.md). Original status labels below describe the old state only.

> **Status:** Draft\
> **Last Updated:** June 2026

---

## Purpose

This document provides a high-level overview of the entities that will likely exist in the LMS. Each entity listed here should eventually be documented in detail using the entity template.

This is a **brainstorming document**, not a schema definition.

---

## Likely Entities

### User

- Represents any person with an account on the platform
- Holds a **set** of account roles: Learner, Course Author, Cohort Coordinator, Learning Facilitator, Administrator, Organization Manager (see [user.md](../../03-data-model/user.md) and [roles-and-permissions.md](../../01-domain/roles-and-permissions.md))
- Links to an Organization (representing the tenant organization, e.g., s2c)

### Organization

- Represents the client tenant entity (e.g., s2c)
- Holds fields for active seat capacity, license keys, subscription status, and expiration timestamps

### Course

- The primary learning container
- Has title, description, metadata, status (draft/published/archived)
- Belongs to a single organization (tenant-scoped)
- Has many cohorts (for hybrid/cohort-based learning)

### Module

- A logical grouping within a course
- Has title, description, ordering position
- Contains lessons

### Lesson

- A single unit of learning
- Has title, content type (Notes / Required Readings), ordering position
- Fields: `lesson_notes` (Rich Text), `required_readings` (JSON array of external resource URLs and text descriptions)

### Forum Thread

- Represents a discussion topic created within a Course Forum or Module Forum
- Fields: `id`, `title`, `scope` (Course vs. Module), `scope_id` (Course ID or Module ID), `author_id`, `created_at`

### Forum Post

- Represents a message/reply within a Forum Thread
- Fields: `id`, `thread_id`, `parent_post_id` (for nested replies), `content` (Markdown), `author_id`, `created_at`

### Quiz

- Represents an assessment associated with a lesson
- Fields: `id`, `lesson_id`, `title`, `type` (Practice vs. Graded), `passing_score_percentage`, `max_attempts` (null for practice)

### Quiz Question

- Represents a single question in a quiz
- Fields: `id`, `quiz_id`, `type` (Single Choice, Multi-Select, True/False), `prompt_text`, `points`

### Quiz Option

- Represents an answer choice for a question
- Fields: `id`, `question_id`, `option_text`, `is_correct` (boolean)

### Quiz Submission

- Tracks a learner's attempt at a quiz
- Fields: `id`, `user_id`, `quiz_id`, `attempt_number`, `score_percentage`, `passed` (boolean), `submitted_at`

### Enrollment

- Links a learner (User) to a specific Cohort (which belongs to a Course)
- Tracks enrollment status (enrolled, in progress, completed, dropped) and dates
- Fields: `id`, `user_id`, `cohort_id` (references Cohort ID), `status`, `enrolled_at`

### Cohort

- Represents a synchronized learning group for a specific course
- Fields: `id`, `course_id` (references Course ID), `title`, `start_date`, `end_date`, `created_at`
- Staff (Cohort Coordinators / Learning Facilitators) are recorded via the [`cohort_staff`](../../03-data-model/cohort-staff.md) join rather than a single `coordinator_id` field

### Progress

- Tracks a learner's completion status per lesson
- Fields: `id`, `user_id`, `lesson_id`, `completed` (boolean), `completed_at`

### Certificate

- Issued upon course completion
- Fields: `id`, `user_id`, `course_id`, `issue_date`, `verification_id` (unique cryptographic hash)

---

## Entity Relationship Overview (Draft)

```text
Organization (Licensing)
 └── User (roles: Learner / Course Author / Cohort Coordinator / Learning Facilitator / Manager)
 └── Course
      ├── Cohort (Cohort Staff ↔ Cohort via cohort_staff)
      │    └── Enrollment (User ↔ Cohort)
      ├── Forum Thread (Scope: Course vs. Module. Filtered by Cohort)
      │    └── Forum Post (User replies)
      └── Module
           ├── Forum Thread (Module-scoped)
           └── Lesson
                ├── Progress (User ↔ Lesson)
                └── Quiz
                     └── Quiz Question
                          └── Quiz Option
                     └── Quiz Submission (User attempts)
                          └── Certificate (issued on quiz completion/passing)
```

---

## Open Questions

- Should we store quiz submission answers in detail (question-by-question selections) for audit trails, or is the final score/grade sufficient?
- Do we need a separate "Media Asset" entity to track image attachments posted in forums?
- How are deactivated users handled in seat count aggregates (e.g., soft-delete vs. hard-delete)?

---

## Related Documents

- [Core Concepts](../../01-domain/core-concepts.md)
- [Entity Template](../../03-data-model/entity-template.md)
- [Glossary](../../01-domain/glossary.md)
