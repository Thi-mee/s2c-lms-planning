# Initial Entities

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document provides a high-level overview of the entities that will likely exist in the LMS. Each entity listed here should eventually be documented in detail using the entity template.

This is a **brainstorming document**, not a schema definition.

---

## Likely Entities

### User

- Represents any person with an account on the platform
- Has a role (or roles): Learner, Instructor, Administrator, Organization Manager
- Has profile information, credentials, and preferences

### Course

- The primary learning container
- Has title, description, metadata, status (draft/published/archived)
- Belongs to an instructor (or multiple instructors?)

### Module

- A logical grouping within a course
- Has a title, description, and ordering position
- Contains lessons

### Lesson

- A single unit of content
- Has a title, content (type varies), and ordering position
- Belongs to a module

### Enrollment

- Links a user (learner) to a course
- Tracks enrollment status and dates

### Progress

- Tracks a learner's completion of lessons, modules, and courses
- May include time spent and scores

### Assessment

- An evaluation associated with a lesson, module, or course
- Contains questions or tasks
- Produces a result/score

### Assessment Submission

- A learner's submitted response to an assessment
- Links to the assessment and the learner
- Has a score and status (submitted, graded)

### Certificate

- Issued upon course completion
- Links to a learner and a course
- Has a unique verification ID

### Organization

- A group of users managed together
- Has members with assigned roles
- May have assigned courses

### Notification

- A system-generated message to a user
- Types: enrollment confirmation, course update, assessment result, etc.

### Category / Tag

- Used to organize and filter courses

---

## Entity Relationship Overview (Draft)

```text
User ──▸ Enrollment ──▸ Course
User ──▸ Progress ──▸ Lesson
User ──▸ Certificate ──▸ Course
Course ──▸ Module ──▸ Lesson
Lesson ──▸ Assessment
User ──▸ Assessment Submission ──▸ Assessment
User ──▸ Organization
```

---

## Open Questions

- Should "Content" be its own entity separate from Lesson?
- Should the system support content reuse across courses?
- Is there a "Course Template" entity for duplicating course structures?
- How are media files (videos, documents) stored and referenced?

---

## Related Documents

- [Core Concepts](../01-domain/core-concepts.md)
- [Entity Template](./entity-template.md)
- [Glossary](../01-domain/glossary.md)
