# Roles and Permissions

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document defines the user roles in the LMS and their associated permissions. This is a conceptual model — the actual permission implementation will be defined during the engineering phase.

---

## Roles

| Role | Description | Primary Actions |
|------|-------------|----------------|
| **Learner** | Consumes educational content | Browse courses, enroll, view lessons, take assessments, track progress |
| **Instructor** | Creates and manages courses | Create/edit courses, manage content, view learner progress in their courses |
| **Administrator** | Manages the platform | Manage users, manage all courses, view analytics, configure settings |
| **Organization Manager** | Manages a group of learners | Assign courses, track team progress, manage team members |

> **Open Question:** See [Q10](../00-product/open-questions.md) — Is Organization Manager a day-one role?

---

## Permission Matrix (Draft)

| Action | Learner | Instructor | Admin | Org Manager |
|--------|---------|------------|-------|-------------|
| Browse published courses | ✅ | ✅ | ✅ | ✅ |
| Enroll in a course | ✅ | ✅ | ✅ | ✅ |
| View lesson content | ✅ (enrolled) | ✅ (own courses) | ✅ | ✅ (org courses) |
| Create a course | ❌ | ✅ | ✅ | ❌ |
| Edit a course | ❌ | ✅ (own) | ✅ (all) | ❌ |
| Delete a course | ❌ | ❌ | ✅ | ❌ |
| View learner progress | ❌ (own only) | ✅ (own courses) | ✅ (all) | ✅ (org members) |
| Manage users | ❌ | ❌ | ✅ | ✅ (org members) |
| View platform analytics | ❌ | ❌ | ✅ | ❌ |
| Manage system settings | ❌ | ❌ | ✅ | ❌ |

> **Assumption:** A user can hold multiple roles (e.g., an instructor can also be a learner). This assumption needs validation.

---

## Open Questions

- Can permissions be customized beyond predefined roles?
- Is there a "super admin" or "platform owner" role above Administrator?
- Should instructors be able to grant other users instructor access to their courses (collaborators)?
- How are permissions scoped in a multi-organization setup?

---

## Related Documents

- [Target Users](../00-product/target-users.md)
- [Glossary](./glossary.md)
- [User Journeys](./user-journeys.md)
