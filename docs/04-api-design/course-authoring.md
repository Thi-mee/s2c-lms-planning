# Course authoring HTTP contract

> **Status:** Implemented first-publication slice; full authoring feature remains partial\
> **Authority:** Canonical — current HTTP contract under the domain/feature rules\
> **Updated:** 2026-09-18

## Purpose and scope

Create an owned draft, save its ordered modules/text lessons, validate and publish it. The [feature](../02-features/course-and-module-administration.md) and [single permission matrix](../01-domain/roles-and-permissions.md) govern. Course Author account membership and the course owner grant are separate records.

All endpoints require a current active session in the configured organization. Every POST requires the anti-forgery cookie and `X-CSRF-TOKEN`. Unknown JSON fields are rejected. The same-origin frontend uses the [identity contract](identity-foundation.md); there is no public catalog, enrollment side effect or approval queue.

## Endpoints

| Method/path | Input | Result/authority |
|---|---|---|
| `GET /api/authoring/courses?offset=0` | Offset 0–100000 | Up to 50 summaries, ordered by creation descending then ID. Administrator/Manager see organization courses; Authors see owned courses; Coordinators also see published courses for cohort launch. Learner/Facilitator alone cannot list. |
| `GET /api/authoring/courses/{id}` | Course ID | Detail and ordered content under the same read scope. Unauthorized/foreign/absent course returns 404. |
| `GET /api/authoring/owners?search=…` | Email substring | Up to 50 active eligible Authors/Administrators, normalized email then ID order. Available to Authors, Managers and Administrators for explicit ownership operations. No account mutation. |
| `POST /api/authoring/courses` | `{ id, title, description, ownerId }` | Create draft and one owner atomically; return detail (200). Author creates for self; Administrator may choose another eligible same-org owner. Manager alone cannot create. |
| `POST /api/authoring/courses/{id}/draft` | `{ expectedRevision, title, description, modules }` | Save the complete ordered draft; owner with Author role or Administrator. Return detail (200). |
| `POST /api/authoring/courses/{id}/publish` | `{ expectedRevision }` | Validate and publish an owned draft/Administrator's organization course; return detail (200). |
| `POST /api/authoring/courses/{id}/owner` | `{ expectedRevision, ownerId, reason }` | Explicit transfer by owning Author, Administrator or Manager; active eligible same-org target. Returns transition detail (200). Existing content is retained. |

Course detail is `{ course, modules }`. A course summary has `id`, `title`, `description`, `status`, `ownerId`, `revision`, `requirementsVersion`, `createdAt`. A module is `{ id, title, lessons }`; a lesson is `{ id, title, notes, required }`. Array order defines positions. UUIDs are client-generated for stable retries, but IDs never confer authority. Timestamps and state/version fields are server-owned.

The first editor supports plain-text notes, rendered as escaped text with line breaks. Stored `notes_format = plain_text` prevents a future rich-text renderer from assuming existing notes are trusted HTML. Rich text, reading links, quiz definitions, archiving and live-course edits are later work; this contract does not claim the whole authoring MVP is implemented. Staff reading these notes writes no learning progress.

## Validation, revisions and retry

- Nonempty titles: at most 200 characters. Description: at most 4,000. Draft: at most 50 modules/200 lessons, 50,000 characters per lesson and 500,000 total note characters. Kestrel bounds request bodies to 1 MB. These are initial engineering limits, not commercial entitlements.
- IDs must be nonempty and unique within the submitted module/lesson lists. A module/lesson ID from another course cannot be repurposed. A lesson's parent module cannot be changed by a draft save. Draft removal is explicit in the submitted structure; published content cannot be changed or removed through this endpoint.
- New lessons in the UI default to visibly checked `required = true`; the author may uncheck it. The API requires an explicit boolean. Publication requires an eligible owner, at least one module, a lesson in every module, nonempty notes for every lesson and at least one required lesson. Assessment validation joins publication when Assessment is implemented; no quiz can currently be authored or published.
- Save/publish/transfer use `expectedRevision`. A stale competing edit returns 409 `revision_conflict`; the UI preserves local edits. Save or discard edits before navigation. Publication returns actionable errors for missing lessons/content/required denominator. There is no success for an empty completion denominator.
- Creating the same course ID with the same original actor/payload returns the existing result; changing that payload returns 409 `request_conflict`. Same save actor/payload/expected revision can be retried without duplicating revisions or audit. Repeated publication of the same revision is idempotent. Authority still applies to every retry.
- Transfer requires a nonempty reason up to 1,000 characters. Concurrent transfers use revision checks; exactly one wins. After transferring away, a former owner without another allowed role cannot continue reading/editing. This resource grant never changes account roles.

## Persistence and module boundary

Authoring owns `authoring.courses`, `course_authors`, `modules`, `lessons` and migration 2. Identity owns account queries, role state and audit persistence. Authoring consumes `IIdentityAccess` and its local `IdentityWork` transaction; it cannot access Identity's internal EF entities. Authorization is rechecked after taking the organization's security-write row lock, shared with role changes/deactivation. Authoring SQL uses the same connection/transaction for its own schema; required audit uses Identity's operation before commit.

The coarse organization write guard deliberately keeps the current monolith simple. It serializes authoring and security writes; it is not a throughput claim. Reads use scoped queries and a repeatable-read snapshot for multi-query course detail. There is no cross-module HTTP, broker, separate database or eventual consistency.

Composite foreign keys enforce organization/course/module ownership. A deferred reverse foreign key plus a unique course-owner grant enforces exactly one owner at commit. Order constraints are deferred within draft saves to allow atomic reordering. No cascading deletes are defined. Runtime cannot delete courses/owner history; content deletion is restricted to the unpublished draft command.

Audit actions are `course.created`, `course.draft_saved`, `course.published`, `course.owner_changed`, and the Identity membership actions. Audit includes actor, target and version/ownership/count changes, not lesson bodies or credentials. Audit persistence failure rolls back the course transition.

## Role-management addition

`GET /api/administration/users?search=…` lets Administrators/Managers find up to 50 active accounts by email, normalized email then ID order. `POST /api/administration/users/{id}/course-author-role` accepts `{ granted, reason, currentPassword? }` and returns 204. It exposes only Course Author membership; it cannot modify the full role set or add Learner without a license guard.

The explicit delegation policy applies. Managers may change ordinary accounts or their own Course Author membership; they cannot change another Administrator/Manager through this command. An Administrator changing another Administrator's operational membership must reauthenticate. Account-role changes increment security version, remove sessions and audit atomically. A self-change signs the caller out. Course ownership records survive loss of the account role; they confer no editing access by themselves, and an authorized explicit transfer can repair ownership before publication. The People screen implements this narrow membership workflow, not general account provisioning.

## Validation and feature gates

[Authoring integration tests](../../tests/Variable.IntegrationTests/AuthoringTests.cs) cover owner and organization isolation, grant revocation, malicious payloads, retry/conflict, publication requirements, atomic audit, ownership constraints, transfer races and v1→v2 migration. [Browser tests](../../src/web/tests/authoring.spec.ts) cover the editor, validation, persisted content, plain-text safety and role-change sign-in.

[Q74](../00-product/open-questions.md#q74) still gates live-definition changes. [Q69](../00-product/open-questions.md#q69) gates archive/access boundaries; [Q70](../00-product/open-questions.md#q70) gates learner completion. No learner or historical-record semantics are changed by first publication. See [local development](../06-architecture/local-development.md) for setup, upgrade and current limitations.
