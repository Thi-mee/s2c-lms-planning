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

- A lesson belongs to exactly one module
- A lesson contains **content** (text, video, file, interactive element, etc.)
- A lesson may have an associated **assessment**
- Lessons are ordered within a module
- A lesson may be marked as required or optional

---

## Enrollment

An **enrollment** represents a learner's registration in a course.

- An enrollment links a **learner** to a **course**
- An enrollment has a status: enrolled → in progress → completed → dropped
- An enrollment tracks the learner's **progress** through the course
- Enrollment may be self-initiated or assigned by an administrator/organization

---

## Progress

**Progress** tracks a learner's advancement through course content.

- Progress is tracked per lesson, per module, and per course
- A lesson is either not started, in progress, or completed
- Module progress is derived from lesson completion
- Course progress is derived from module completion
- Progress may include assessment scores

---

## Assessment

An **assessment** evaluates a learner's understanding.

> **Open Question:** See [Q8](../00-product/open-questions.md) — Will there be assessments?

- An assessment may be a quiz, assignment, or other exercise
- An assessment is associated with a lesson, module, or course
- An assessment produces a score or result
- Assessments may be required for course completion

---

## Certificate

A **certificate** is a credential issued upon course completion.

> **Open Question:** See [Q9](../00-product/open-questions.md) — Will certificates be generated?

- A certificate is issued to a learner for a specific course
- A certificate may have a unique identifier for verification
- Certificate issuance may depend on assessment scores or completion requirements

---

## Concept Relationships

```text
Course
 └── Module
      └── Lesson
           └── Content
           └── Assessment (optional)

Learner ──enrolls in──▸ Course
         ──has──▸ Progress
         ──earns──▸ Certificate
```

---

## Open Questions

- Should courses support branching or non-linear paths?
- Can a lesson belong to multiple modules or courses (shared content)?
- How is content versioning handled?
- Are modules required, or can a course have a flat list of lessons?

---

## Related Documents

- [Glossary](./glossary.md)
- [Roles and Permissions](./roles-and-permissions.md)
- [User Journeys](./user-journeys.md)
