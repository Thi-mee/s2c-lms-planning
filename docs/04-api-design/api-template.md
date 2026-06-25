# API: [API Area / Endpoint Group]

> **Status:** Draft | In Review | Confirmed  
> **Last Updated:** [Date]

---

## Purpose

What does this API enable? What problem does it solve?

---

## Actor

Which user type(s) will call this API?

- [ ] Learner
- [ ] Instructor
- [ ] Administrator
- [ ] Organization Manager
- [ ] System (internal/automated)

---

## Endpoints

### [HTTP Method] [Path]

**Purpose:** [What this endpoint does]

**Request:**

```json
{
  "field": "type — description"
}
```

**Response (Success):**

```json
{
  "field": "type — description"
}
```

**Response (Error):**

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  }
}
```

---

## Permissions

| Endpoint | Learner | Instructor | Admin | Org Manager |
|----------|---------|------------|-------|-------------|
| [Endpoint] | ✅/❌ | ✅/❌ | ✅/❌ | ✅/❌ |

---

## Validation

- [Field] must be [constraint]
- [Field] must be [constraint]

---

## Error Cases

| Scenario | Error Code | HTTP Status |
|----------|------------|-------------|
| [Scenario] | [Code] | [Status] |

---

## Pagination

Does this endpoint return paginated results? If so, describe the pagination approach.

---

## Idempotency

Is this endpoint idempotent? Are there any considerations for retry safety?

---

## Open Questions

- Question 1
- Question 2

---

## Related Documents

- [Link to related feature]
- [Link to related entity]
