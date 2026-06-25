# Entity: [Entity Name]

> **Status:** Draft | In Review | Confirmed  
> **Last Updated:** [Date]

---

## Purpose

What does this entity represent? Why does it exist in the system?

---

## Key Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| id | identifier | Yes | Unique identifier |
| [field] | [type] | [Yes/No] | [Description] |

> **Note:** Field types are conceptual (e.g., "text", "identifier", "timestamp", "enum", "reference"). They are not tied to a specific database type yet.

---

## Relationships

| Related Entity | Relationship | Description |
|---------------|-------------|-------------|
| [Entity] | [has many / belongs to / has one] | [Description] |

---

## Lifecycle

Describe the states this entity goes through.

```text
[State 1] → [State 2] → [State 3]
```

---

## Business Rules

- Rule 1
- Rule 2

---

## Validation Rules

- [Field] must be [constraint]
- [Field] must be [constraint]

---

## Permissions

Who can create, read, update, and delete this entity?

| Action | Learner | Instructor | Admin | Org Manager |
|--------|---------|------------|-------|-------------|
| Create | ✅/❌ | ✅/❌ | ✅/❌ | ✅/❌ |
| Read | ✅/❌ | ✅/❌ | ✅/❌ | ✅/❌ |
| Update | ✅/❌ | ✅/❌ | ✅/❌ | ✅/❌ |
| Delete | ✅/❌ | ✅/❌ | ✅/❌ | ✅/❌ |

---

## Audit / History Requirements

Should changes to this entity be tracked? What audit information is needed?

---

## Open Questions

- Question 1
- Question 2

---

## Related Documents

- [Link to related feature]
- [Link to related entity]
