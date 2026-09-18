# Roles, grants and privileged delegation

> **Status:** Confirmed baseline; quiz-preview exception explicitly gated\
> **Authority:** Canonical — the only account/resource permission matrix\
> **Updated:** 2026-09-17

## Purpose

Encode accepted delegation and resource boundaries without numeric role hierarchies. Entity/feature documents reference this matrix instead of maintaining competing CRUD tables.

## Two layers and evaluation

Account roles are a set: Learner, Course Author, Cohort Coordinator, Learning Facilitator, Organization Manager, Administrator. Course Author uses `course_authors`; Coordinator/Facilitator use `cohort_staff`; Learner uses `enrollments`. A matching contextual grant without the current account role does not authorize that role's access. Explicit organization-wide Administrator permissions below remain a separate override. Roles combine by explicit allowed actions, never `actorRole > targetRole`.

Every action requires an active authenticated account, the same organization, and current account/resource authorization. Administrator means within the installation's organization, not a vendor superuser. Account deactivation or role/grant revocation invalidates relevant access. Staff who want to learn need Learner and an enrollment; test preview is a separate mode.

## Account-role grants

The following matrix covers **both grant and revoke**. `Yes` never bypasses organization or target-account protections.

| Role to change | Organization Manager | Administrator via ordinary role management | Dedicated Administrator-security operation | Other account roles |
|---|---|---|---|---|
| Learner | Yes | Yes | Not needed | No |
| Course Author | Yes | Yes | Not needed | No |
| Cohort Coordinator | Yes | Yes | Not needed | No |
| Learning Facilitator | Yes | Yes | Not needed | No |
| Organization Manager | **No** | Yes | Not needed | No |
| Administrator | **No** | **No** | Existing usable Administrator, explicit grant/revoke command, fresh authentication, audit and continuity guard | No |

Engineering realization of “explicitly authorized”: Administrator membership changes use a dedicated security command, never the generic user/role PATCH/import endpoint. The initiating existing Administrator must intentionally request that operation and reauthenticate. Initial establishment is operator bootstrap, not ordinary role delegation. No unapproved approval committee or numeric rank is introduced.

An Organization Manager may change only the four ordinary memberships; an API accepting the full desired role set must reject any attempt to add/remove privileged memberships. Invitations, batch actions, resource-grant actions and self-edits use the same policy. Giving oneself Course Author is operational authority permitted by the accepted policy; giving oneself Organization Manager or Administrator is forbidden.

Adding Learner to an active account uses the atomic capacity guard. Removing it releases a learner seat and learning access without deleting history. Do not infer multiple seats from multiple roles.

## User/security operations and continuity

- Organization Managers manage ordinary accounts, invitations and allowed role memberships. They cannot change the credentials, login email, active/deleted state or security settings of an Administrator/Organization Manager through general user administration. This prevents indirect takeover, demotion or lockout. A privileged user retains normal self-service password/profile flows subject to identity controls.
- Administrators can manage ordinary and Organization Manager accounts. Security-sensitive changes to another Administrator use the dedicated Administrator-security path.
- Editable own-profile fields are explicitly allowlisted; a profile endpoint cannot alter roles, organization, grants, status, security version or license-derived fields.
- Removing Administrator, deactivating/deleting an Administrator, or disabling its only usable credential must not remove the final usable Administrator. Serialize concurrent changes. Pending/inactive/deleted Administrators do not satisfy the guard. No unspecified recovery exception may bypass it.
- Persist actor, target, before/after role/security change and outcome with the business transaction; redact secrets. Invalidate affected sessions/security versions. Current authority is rechecked within sensitive mutations.

## Resource-grant authority

| Resource operation | Allowed actor | Boundaries |
|---|---|---|
| Establish/assign course owner | Creator with Course Author or Administrator; Administrator/Organization Manager may assign an eligible owner | Same org; exactly one owner; creation and owner grant atomic. Manager role alone does not permit content editing |
| Transfer course ownership | Current owning Course Author, Administrator, Organization Manager | Eligible same-org owner; atomic replacement; no ownerless course or collaborator feature |
| Assign/revoke cohort staff | Coordinator of that cohort, Administrator, Organization Manager | Same org; target already holds corresponding account role; preserve active-cohort Coordinator requirement |
| Create cohort | Cohort Coordinator, Administrator, Organization Manager | Published same-org course; creator may receive Coordinator grant only if eligible; assign eligible Coordinator before activation |
| Enroll/withdraw learner | Coordinator of that cohort, Administrator, Organization Manager | Same org; current Learner entitlement for enrollment; active account; one active run/user/course; withdrawal/access details Q69 |
| Grant any resource scope | Only actors listed above | Never grants an account role implicitly; resource grant cannot confer org security authority |

For ownership assignment, an eligible owner is a same-organization account holding Course Author or Administrator, consistent with Administrator authoring permission. This resource grant never supplies a missing account role. Organization Manager cannot create a course through ownership assignment alone; creation still requires the Author/Administrator action.

A Coordinator may select an existing eligible learner but cannot create accounts or assign Learner by virtue of coordinating. A Facilitator or Course Author does not manage cohort membership without a separately held authorized role/grant. Removing an account role makes its contextual grants ineffective; preserve grant/history records and enforce remaining-owner/staff constraints through explicit workflows.

## Product actions

`Own cohort` refers to the enrollment or staff grant for the **record's cohort**, not any cohort the user is currently associated with.

| Action | Learner | Facilitator | Coordinator | Course Author | Administrator | Organization Manager |
|---|---|---|---|---|---|---|
| Read learning content | Assigned run | Staffed cohort | Staffed cohort; published library for cohort launch | Own course | Org | Org |
| Author modules/lessons/quizzes; publish/archive | No | No | No | Own course | Org | No without Author role/grant |
| Configure cohort/membership | No | No | Own cohort | No | Org | Org |
| Record lesson progress / real quiz attempt | Own active run | Only with Learner + own run | Only with Learner + own run | Only with Learner + own run | Only with Learner + own run | Only with Learner + own run |
| Read own progress/results | Own run | As Learner if enrolled | As Learner if enrolled | As Learner if enrolled | As Learner if enrolled | As Learner if enrolled |
| View cohort learning progress | No others | Own cohort | Own cohort | Own course's runs | Org | Org aggregate/progress |
| View detailed quiz answers | Own, per Q73 | Own cohort | Own cohort | Own course | Org | No via Manager alone |
| Reset graded allowance | No | Own cohort | Own cohort | Own course | Org | No |
| Read/create discussion | Own cohort | Own cohort | Own cohort | Own course's cohorts | Org | Org |
| Moderate discussion | No | Own cohort | Own cohort | Own course's cohorts | Org | No |
| Edit/delete own post | Yes in authorized scope | Yes in authorized scope | Yes in authorized scope | Yes in authorized scope | Yes | Not via Manager alone (existing MVP restriction) |
| Receive automatic certificate | Qualified completed run; once/user/course | Only when enrolled as Learner | Same | Same | Same | Same |
| Void/reissue certificate | No | No | No | No | Explicit audited operation; Q75 | No |
| Configure organization/certificate branding | No | No | No | No | Yes | Yes |
| Read license/capacity; organization analytics/audit | No | No | No | No | Yes | Yes, org scope |
| Change license-derived values | No | No | No | No | No; signed file only | No; signed file only |
| System settings | No | No | No | No | Yes | No |

Quiz preview/test roles and learner feedback visibility remain [Q73](../00-product/open-questions.md#q73). Do not resolve the former contradictory Coordinator test permission by counting previews as real attempts. Old entity permission matrices are superseded by this document.

## Validation obligations

[Invariants I1–I4 and I10](../06-architecture/implementation-invariants.md) require negative API tests, concurrency checks, current-resource scope tests and crafted payload tests. Derive navigation capabilities from the same policy but always enforce server-side.

## Open questions

[Q69](../00-product/open-questions.md#q69): boundary access/withdrawal. [Q73](../00-product/open-questions.md#q73): test/review. [Q75](../00-product/open-questions.md#q75): replacement credentials. There is no unresolved multi-role or privileged-delegation decision.

## Related documents

[Accepted decisions](../00-product/accepted-decisions.md) · [Security and identity](../06-architecture/security-and-identity.md) · [Core concepts](core-concepts.md)
