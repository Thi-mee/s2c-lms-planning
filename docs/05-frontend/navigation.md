# Web navigation and walking-skeleton screens

> **Status:** Confirmed behavior baseline; information architecture is an engineering default\
> **Authority:** Supporting UI specification, derived from domain and features\
> **Updated:** 2026-09-17

## Purpose

Use one adaptive application shell for accounts holding multiple roles. Visible actions reflect current account and resource capabilities; server authorization remains authoritative. Routes identify the selected learning run wherever learning evidence is read or written.

## Navigation map

| Area | Screens and access |
|---|---|
| Entry | Login, invite acceptance, password recovery; expired/used invitation and capacity-full states. Controlled operator bootstrap is a separate first-install operation, not a public signup page. |
| My learning | Assigned cohorts/current runs, course → module → lesson with breadcrumbs, practice/graded quiz, own run results/progress, historical runs, own certificates. Requires applicable Learner/run access. |
| Authoring | Owned course list, create draft, modules/lessons, quiz editor, publication validation. Administrator may manage organization courses. |
| Cohorts | Published course selection for launch, schedule/staff, roster assignment, cohort progress and scoped forums. Coordinator/Administrator/Manager actions follow the resource policy; Facilitator support view omits enrollment controls. |
| People | Invite, pending/active/inactive accounts, role sets, deactivation/reactivation and capacity. Manager can select only permitted ordinary grants and cannot mutate privileged-account security. |
| Administration | Organization branding, signed-license status/loading, audit, system settings according to policy. Administrator membership changes have a dedicated reauthentication workflow. |
| Profile | Allowlisted own profile/password/session actions. No role/status/org fields hidden in a general profile form. |

There is no learner Browse/Enroll catalog, self-registration approval queue, publishing approval queue, sequential lesson lock or in-app notification center. Course/module forums are reached in the current cohort context. Search/filter controls are contextual; no global search engine is implied. The ordinary single-organization installation has no customer/tenant switcher or vendor billing interface.

## First end-to-end path

```text
Operator establishes organization + staff-only Administrator
  → Administrator logs in and grants/assigns Course Author
  → Author creates and publishes a course
  → authorized staff schedule a cohort and its Coordinator
  → Manager/Admin invites Learner; invitee activates within capacity
  → Coordinator/Admin/Manager assigns a fresh learning run
  → Learner opens lesson and records the decided reading signal
  → Learner submits graded quiz and sees permitted result
  → run completion and existing/new course certificate are shown
```

Use explicit selected Enrollment identity on the learning screen. If a user has multiple historical runs, label their cohort/date/status and do not merge their evidence into one progress bar. A user/course summary can link to those records. A subsequent completed run can link to the existing course certificate without claiming it was newly issued for that run.

## Required UI states

- Loading, empty assigned-cohort list, pending/expired invitation, permission loss and deactivated-session redirect.
- Capacity full at invitation precheck or acceptance; preserve recoverable entered data without promising a reserved seat.
- Draft/publish validation errors, forbidden ownership/grants and final-Administrator protection.
- Current/upcoming/ended cohort indicators; Q69 determines actual boundary access.
- Attempt remaining/exhausted/submitting/already-submitted states per Q72; retries must not submit twice. Never preload answer keys into hidden client state.
- Completion processing/failure recovery if work is asynchronous; distinguish an issued credential from PDF rendering or email delivery. PDF errors do not imply learning was lost.
- Keyboard operation, semantic controls, clear focus/error messages and responsive layouts. Formal accessibility target is Q41.

## Open questions

Link screen acceptance to [Q69–Q75](../00-product/open-questions.md), especially reading signal, attempt lifecycle, feedback and reissue. Wireframes/component library and route naming are implementation choices; no new design system or speculative screen catalog is required in this pass.

## Related documents

[Role policy](../01-domain/roles-and-permissions.md) · [User journeys](../01-domain/user-journeys.md) · [Features](../02-features/README.md) · [Implementation plan](../07-roadmap/implementation-plan.md)
