# Navigation Structure

> **Status:** Draft  
> **Last Updated:** June 2026

---

## Purpose

This document outlines the planned navigation structure for the LMS. Navigation may differ based on user role.

---

## Learner Navigation (Draft)

```text
├── Dashboard (Home)
├── Browse Courses
│   └── Course Detail
│       └── Enroll
├── My Courses
│   └── Course Player
│       ├── Module View
│       └── Lesson View
├── My Progress
├── My Certificates
├── Notifications
└── Profile / Settings
```

---

## Instructor Navigation (Draft)

```text
├── Dashboard (Home)
├── My Courses
│   ├── Create Course
│   └── Edit Course
│       ├── Manage Modules
│       ├── Manage Lessons
│       └── Manage Assessments
├── Learner Progress
├── Notifications
└── Profile / Settings
```

---

## Administrator Navigation (Draft)

```text
├── Dashboard (Home)
├── User Management
│   ├── All Users
│   └── Invite User
├── Course Management
│   └── All Courses
├── Organization Management
├── Analytics & Reports
├── System Settings
├── Notifications
└── Profile / Settings
```

---

## Open Questions

- Should learners and instructors share a single navigation with conditional items, or have separate layouts?
- Where does search live in the navigation?
- Is there a global notification center?
- Should there be a breadcrumb trail for deep navigation?

---

## Related Documents

- [Screen Template](./screen-template.md)
- [User Journeys](../01-domain/user-journeys.md)
- [Roles and Permissions](../01-domain/roles-and-permissions.md)
