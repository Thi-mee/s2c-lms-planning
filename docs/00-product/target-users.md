# Target Users

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document identifies the types of users the LMS is being designed for. User definitions will influence features, permissions, and interface design.

---

## Identified User Types

> **Draft:** These are initial user types. They may be renamed, merged, or expanded.

### Learner

- The primary consumer of educational content
- Enrolls in courses, completes lessons, takes assessments
- Tracks their own progress
- May receive certificates upon completion

### Instructor

- Creates and manages course content
- May monitor learner progress within their courses
- May grade assessments or provide feedback
- Could be a subject-matter expert, teacher, or trainer

### Administrator

- Manages the platform itself
- Creates and manages user accounts
- Configures system settings
- Views platform-wide analytics and reports
- May manage courses, categories, or organizational structure

### Organization Manager

- Represents the client organization (tenant) buying or deploying the platform
- Manages the subscription, licensing, and student seat limits (assigned vs. available seats)
- Registers or invites learners and instructors within their organization
- Assigns courses to learners and monitors organizational learning reports

### Learning Coordinator

- Oversees specific course **cohorts** (especially for pre-designed, trainerless courses)
- Has complete access to course content to assist learners
- Facilitates the **course forum** and module forums to support cohort interaction, answer questions, and drive engagement
- Monitors progress and completion metrics for their assigned cohort members

---

## Open Questions

- Should we support self-registration for learners where the Organization Manager only approves them?
- Can a single user account hold multiple roles (e.g., an Instructor who is also enrolled as a Learner)?
- Will there be a read-only Auditor role for external compliance reviewers?

---

## Related Documents

- [Roles and Permissions](../01-domain/roles-and-permissions.md)
- [User Journeys](../01-domain/user-journeys.md)
- [Vision](./vision.md)
