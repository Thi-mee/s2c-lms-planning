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
| **Learning Coordinator** | Manages/facilitates course cohorts | View content, facilitate forums, track cohort progress, manage cohort members |
| **Instructor** | Creates and manages courses | Create/edit courses, manage content, view learner progress in their courses |
| **Administrator** | Manages the platform | Manage users, manage all courses, view analytics, configure settings |
| **Organization Manager** | Manages a group of learners and billing/seat capacities | Invite/manage learners and instructors, assign courses, track progress, review license capacity |

---

## Permission Matrix (Draft)

| Action | Learner | Coordinator | Instructor | Admin | Org Manager |
|--------|---------|-------------|------------|-------|-------------|
| Browse published courses | ✅ | ✅ | ✅ | ✅ | ✅ |
| Enroll in a course | ✅ | ❌ | ✅ | ✅ | ✅ |
| View lesson content | ✅ (enrolled) | ✅ (cohort) | ✅ (own) | ✅ | ✅ (org members) |
| Create a course / modules | ❌ | ❌ | ✅ | ✅ | ❌ |
| Edit a course / modules | ❌ | ❌ | ✅ (own) | ✅ (all) | ❌ |
| Post to course / module forums | ✅ | ✅ | ✅ | ✅ | ✅ |
| Create practice & graded quizzes | ❌ | ❌ | ✅ | ✅ | ❌ |
| Take quizzes & view scores | ✅ | ✅ (test) | ✅ (practice) | ✅ (test) | ❌ |
| Issue completion certificates | ❌ (auto) | ❌ | ✅ (criteria) | ✅ | ❌ |
| Manage users & invite members | ❌ | ❌ | ❌ | ✅ | ✅ (org members) |
| Manage cohort assignments | ❌ | ✅ | ❌ | ✅ | ✅ |
| View license & seat capacities | ❌ | ❌ | ❌ | ✅ | ✅ |
| View platform analytics | ❌ | ❌ | ❌ | ✅ | ❌ |
| Manage system settings | ❌ | ❌ | ❌ | ✅ | ❌ |

> **Assumption:** A user account holds a single primary role in the database for clean authorization logic, but a user mapped to the "Instructor" or "Coordinator" role can still enroll as a Learner in other courses if granted enrollment access. Cohort assignments allow the Learning Coordinator to assign learners within their organization to a specific cohort.

---

## Open Questions

- Should Organization Managers be able to create custom courses for their organization only, or do they rely entirely on Instructors/Admins?
- Do we need an approval step where an Administrator approves an Organization Manager's request to increase their seat limit?
- Should instructors be able to designate other instructors as co-owners or collaborators on their courses?

---

## Related Documents

- [Target Users](../00-product/target-users.md)
- [Glossary](./glossary.md)
- [User Journeys](./user-journeys.md)
