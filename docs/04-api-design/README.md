# API Design

> **Last Updated:** June 2026

---

## Purpose

This folder contains planning documents for the LMS API surface. These are **not** OpenAPI specifications or implementation code — they are conceptual documents that describe what APIs the system will need.

Actual API implementation will happen during the engineering phase, informed by these documents.

---

## How to Add an API Document

1. Copy `api-template.md` to a new file with a descriptive name (e.g., `course-api.md`)
2. Fill in the template sections
3. Use `TBD` for sections that are not yet defined
4. Add the API to the index below
5. Cross-link to related features and entities

---

## API Index

| API Area | Status | File |
|----------|--------|------|
| *(No APIs documented yet)* | — | — |

APIs will be defined as features are specified and the architecture direction is established.

---

## Design Considerations

> **Open Question:** See [Q13](../00-product/open-questions.md) — Tech stack decisions will influence API design.

Pending decisions:

- REST vs. GraphQL vs. hybrid
- Authentication mechanism (JWT, sessions, OAuth)
- Versioning strategy
- Pagination approach
- Error response format

---

## Related Documents

- [API Template](./api-template.md)
- [Architecture Decisions](../06-architecture/decisions.md)
- [Features](../02-features/README.md)
