# Core Concepts

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document describes the core domain concepts of the LMS. These concepts form the foundation of the product's data model, features, and user experience.

All concepts here are **draft** and subject to refinement as the product direction matures.

---

## Course

A **course** is the primary unit of structured learning. It represents a complete learning experience on a specific topic.

- A course has a title, description, and metadata
- A course is organized into **modules**
- A course may have prerequisites
- A course may be free or paid (TBD — see [Q5](../00-product/open-questions.md))
- A course has a lifecycle: draft → published → archived

---

## Module

A **module** is a logical grouping of lessons within a course. It represents a chapter, unit, or topic area.

- A module belongs to exactly one course
- A module contains one or more **lessons**
- Modules are ordered within a course
- A module may have a summary or learning objectives

---

## Lesson

A **lesson** is a single unit of educational content within a module.

- A lesson belongs to exactly one module.
- A lesson contains **content** (defined conceptually as extensible; MVP implements **Rich Text**, **Required Readings** [which can link to external sources], and **Lesson Notes**).
- A lesson may have an associated **assessment** (practice or graded quiz).
- Lessons are ordered within a module.

---

## Enrollment

An **enrollment** represents a learner's registration in a specific **cohort** of a course.

- An enrollment links a **learner** directly to a **cohort** (which in turn belongs to a **course**).
- An enrollment has a status: enrolled → in progress → completed → dropped.
- An enrollment tracks the learner's **progress** through the course associated with that cohort.
- Enrollment is controlled by the host organization (constrained by seat licensing limits).

---

## Progress

**Progress** tracks a learner's advancement through course content.

- Progress is tracked per lesson, per module, and per course.
- A lesson is marked completed when the learner finishes reading lesson content or successfully submits the associated quiz.
- Course progress is derived from completion of required lessons and passing graded quizzes.

---

## Assessment

An **assessment** evaluates a learner's understanding.

- The system supports two quiz types:
  - **Practice Quiz:** Can be taken multiple times, does not affect course grade, used for self-evaluation.
  - **Graded Quiz:** Has strict submission limits, affects course completion, produces a permanent grade.
- Quizzes consist of multiple-choice, select-all, or true/false questions.

---

## Forum

A **forum** is a communication space for questions and discussions.

- Forums exist at two distinct scopes:
  - **Course Forum:** A general discussion space for the entire course.
  - **Module Forum:** A contextual discussion space focused specifically on the module's topic area.
- Users (Learners, Instructors, Managers) can create threads, post replies, and link to specific lesson concepts.

---

## Certificate

A **certificate** is a credential issued upon course completion.

- Automatically generated for a learner once course completion criteria (required lessons read + passing grade on graded quizzes) are met.
- Includes details such as: course name, learner name, issue timestamp, and a unique cryptographic verification ID.

---

## Seat License

A **seat license** defines the student capacity allowed on a self-hosted instance.

- Set on a per-organization basis by a license key (yearly subscription).
- The system checks active student counts before allowing new learner registrations.
- Includes mechanisms for reporting active seat metrics back to s2c.

---

## Cohort

A **cohort** is a structured group of learners taking a course on a synchronized schedule.

- A cohort belongs to a specific course.
- Learners are assigned to exactly one cohort per course enrollment.
- Interaction between learners is scoped to their cohort, allowing for a shared pace and cohort-scoped discussions in forums.

---

## Learning Coordinator

A **learning coordinator** is a staff role designated to facilitate learning experiences, especially in pre-designed, trainerless courses.

- Assigned to oversee one or more course **cohorts**.
- Has full view access to course materials.
- Facilitates the **course forum** and module forums to answer questions, moderate topics, and manage group dynamics.
- Monitors progress tracking data for their cohort members but does not create or edit course structures.

---

## Concept Relationships

```text
Organization (Seat License)
 └── Course
      ├── Course Forum (Cohort-scoped threads)
      └── Module
           ├── Module Forum
           └── Lesson
                ├── Lesson Notes
                ├── Required Readings (External Links)
                └── Assessment (Practice / Graded Quiz)

Cohort (Assigned to Course)
 ├── Learning Coordinator (Facilitates)
 └── Enrollment (Ties Learner to Cohort)
      ├── Progress (Lesson & Quiz status)
      └── Certificate (Generated on completion)
```

---

## Open Questions

- Should we support lesson prerequisites (e.g., must finish Lesson 1 before opening Lesson 2)?
- How long should quiz scores be preserved in the system logs?
- Do forums support rich text and image attachment uploads?
- How does the system handle learners deactivated to free up seats? Do they lose access to progress records?

---

## Related Documents

- [Glossary](./glossary.md)
- [Roles and Permissions](./roles-and-permissions.md)
- [User Journeys](./user-journeys.md)
