# Run Variable LMS locally

> **Status:** Implemented development slice; not a customer release\
> **Authority:** Supporting operational guide\
> **Updated:** 2026-09-18

## What runs now

The first [walking-skeleton slice](../07-roadmap/implementation-plan.md) establishes an organization and staff-only Administrator, authenticates through the React application, and persists revocable sessions and required audit in PostgreSQL. The identity module also exposes dedicated Administrator-role and deactivation commands for continuity and revocation testing. Their administration UI and ordinary role-management workflows come in later slices.

The second slice adds Course Author membership management, an owned draft editor with ordered modules/plain-text lessons, and direct publication after content/required-lesson validation. The third implemented slice adds Coordinator/Facilitator eligibility, published-course cohort scheduling, atomic initial staffing and scoped staff views. Published content is read-only. Licensing, invitations, enrollment, learning progress, assessments and certificates are not implemented. Use synthetic development identities while the real-data and release gates remain open.

## Prerequisites and first run

Use .NET SDK **10.0.400** (see root `global.json`), Node **24.x** (CI uses 24.19.0), npm, Python 3 for the local password helper, and Docker with Compose v2. The development database binds only `127.0.0.1:54637`; the host uses port 5078 and optional Vite dev server 5178. The Compose project/volume is named `variable-lms-dev` and does not share another application's database.

Run these commands from the repository root, in order. Build the frontend before the backend; both access the backend's `wwwroot` directory.

```sh
dotnet restore Variable.slnx --locked-mode
npm ci --prefix src/web
npm run build --prefix src/web
dotnet build Variable.slnx --no-restore --disable-build-servers -m:1
bash scripts/dev.sh db
bash scripts/dev.sh migrate
bash scripts/dev.sh bootstrap
bash scripts/dev.sh serve
```

Open `http://localhost:5078`. The development account is `administrator@example.test`; its unique generated password is in the ignored, owner-readable `.local/bootstrap-password` file. Inspect it locally in your editor; never paste it into an issue or commit it. This file is deliberately retained for the local browser tests. A non-development operator supplies a password file securely and removes it after bootstrap.

Bootstrap is one-time and rejects a second run. It has no HTTP endpoint and does not grant a Learner entitlement. Restarting the host preserves accounts and sessions when the database and `.local/keys` are preserved. `bash scripts/dev.sh stop-db` stops the development database without deleting its volume. Do not reset the database or change the installation UUID to work around a failed second bootstrap.

For live frontend development, keep the backend running and run `bash scripts/dev.sh web` in a second terminal. Vite proxies `/api` and `/health` to the backend. `public/` at repository root remains the historical/derived dossier; it is never copied into the LMS frontend.

## Validation

```sh
dotnet test Variable.slnx --no-restore --disable-build-servers -m:1
npm exec --prefix src/web -- playwright install chromium
```

For the browser check, stop any manually started host on 5078, then run the following from `src/web`:

```sh
npm run test:e2e
```

Playwright starts and stops its own backend host. It exercises the compiled React application in desktop and mobile Chromium, including rejected login, persistent session/logout, role changes, draft creation/editing, publication validation, safe plain-text rendering and atomic staffed-cohort scheduling. Screenshots go to ignored `src/web/test-results/`; traces/video are disabled so credentials are not recorded. It requires the development bootstrap above. On Linux CI, the workflow installs Chromium's system dependencies too.

The .NET suite uses real PostgreSQL. Each integration test creates a uniquely named `variable_test_<uuid>` database, applies the actual migration, and drops only that database during cleanup. It uses the development operator credentials to create fixtures; application requests use the restricted runtime role. Do not point this harness at a production database server. `VARIABLE_TEST_OPERATOR_CONNECTION` can relocate the isolated test server; it must provide the same synthetic migrator/runtime roles as [init-dev.sql](../../deploy/compose/init-dev.sql).

The [CI workflow](../../.github/workflows/ci.yml) performs locked restores, frontend build, backend tests, explicit migration/bootstrap and browser checks. Third-party actions are pinned to official release commits; no publish or deployment job exists.

## Runtime and operator configuration

ASP.NET Core configuration uses `__` for environment-variable nesting. [scripts/dev.sh](../../scripts/dev.sh) supplies only synthetic local settings; its passwords are not reusable customer credentials. A normal host and bootstrap must not receive migration credentials.

| Key | Required use |
|---|---|
| `ConnectionStrings__Runtime` | Non-owner, non-superuser connection without schema/database CREATE, role creation or BYPASSRLS. |
| `Installation__OrganizationId` | Nonempty UUID, stable for this installation. A mismatch with the bootstrap marker fails readiness. |
| `Installation__PublicOrigin` | Origin URL only; HTTPS outside Development. Local HTTP is accepted only for a loopback origin. |
| `DataProtection__KeyDirectory` | Absolute persistent directory with access restricted to the application/operator. |
| `DataProtection__CertificatePath` | PFX with private key required outside Development and the isolated Testing host, to encrypt persisted keys. |
| `DataProtection__CertificatePasswordFile` | Optional file containing the PFX password; keep out of arguments and logs. |
| `Security__TrustedProxies__0` (and subsequent indices) | Explicit trusted proxy IPs, only when using TLS termination; forwarded headers from arbitrary peers are ignored. |
| `ConnectionStrings__Migration` | Explicit `migrate` command only; never normal runtime/bootstrap. |
| `Database__RuntimeRole` | `migrate` only; name to receive scoped table grants. |
| `Bootstrap__OrganizationName`, `Bootstrap__AdministratorName`, `Bootstrap__Email`, `Bootstrap__PasswordFile` | Explicit one-time `bootstrap` command only. |

Without a command the executable runs the host. `migrate` and `bootstrap` are explicit operator commands. Normal startup performs no DDL. The host composes Identity migration 1, Authoring migration 2 and Enrollment migration 3 through the small `Variable.Database` runner. Module-owned SQL remains embedded in its owning assembly. The `platform.schema_migrations` ledger records immutable SQL checksums, and one transaction-scoped PostgreSQL advisory lock covers the complete migration sequence. It rejects unknown schema versions or modified migration history. The runtime can insert/read audit but cannot update/delete it, cannot delete account/organization/bootstrap history, and cannot perform DDL. Organization UPDATE permission is needed for the security transaction's row lock. This protects against accidental runtime mutation; it is not tamper-proof evidence against a database operator.

`/health/live` reports process liveness; `/health/ready` checks database access, restricted role, the exact expected migration names/versions/checksums and installation binding. Authentication/API persistence failures fail closed with a redacted response. Invalid required configuration fails startup. JSON logs include correlation/request IDs; credentials, ticket contents and EF sensitive parameter logging are not enabled.

## Concrete security defaults

- ASP.NET Core Identity `PasswordHasher` hashes credentials. Bootstrap passwords are 14–128 characters; names are nonempty and at most 200; email is at most 320 and unique after invariant case normalization within the organization.
- Sessions have an absolute eight-hour expiry, no sliding renewal, and PostgreSQL-backed current-account/security-version checks. Security changes delete sessions and increment the version in the business/audit transaction.
- Five failed passwords trigger a 15-minute temporary account lockout; it expires without removing the final Administrator. Login also allows 20 requests per IP per minute; security commands allow five per actor per minute. These process-local rate limits are an initial guard, not a claim of coordinated multi-replica throttling.
- Production cookies are `__Host-`, Secure, HttpOnly and SameSite=Strict. Local Development uses separate `variable-dev-*` cookie names to allow loopback HTTP. CSRF tokens remain required in both modes; no session is stored in browser local storage.
- Administrator changes require current-password reauthentication and a reason. The organization row lock serializes sensitive changes; an already-running request rechecks current authority inside that transaction. Temporary lockout, pending, deleted or credential-less accounts are not usable substitutes for final-Administrator removal.
- Protected key-ring files and their encryption certificate/private key belong in backup coverage. Losing them invalidates protected sessions. Test/development keys are unencrypted on disk and must never be reused for customer installations.

## Upgrade an existing checkout

Stop the local host before applying a missing schema. Back up the database, preserve `.local/keys` and the local credential file, rebuild frontend/backend in the order above, then run `bash scripts/dev.sh migrate` and restart. Do not bootstrap again. The runner verifies the existing checksum prefix and applies only missing migrations: v1→v2 preserves identity while adding Authoring; v2→v3 preserves identity/courses while adding Enrollment & Cohorts. New runtime readiness rejects a database missing v3; an old runtime rejects a database containing a newer migration. A failed migration transaction rolls back; do not delete ledger entries or alter an already-applied SQL file to force it through.

For local database backup, create an ignored `.local/backups/` directory and use `docker compose -f deploy/compose/compose.dev.yml exec -T database pg_dump -U postgres -d variable_lms -Fc` redirected to a new, access-restricted file there. This pass saved `.local/backups/authoring-before-enrollment.dump` before upgrading the development database to v3. Customer backup/restore qualification remains a release gate.

## Authoring defaults and limits

Use **Courses → New course**, select the owner, then add modules and lessons in the draft editor. New lessons visibly default to required. Save before publishing. The server checks current account and course scope, missing notes/lessons and a nonzero required-lesson count. Draft writes are serialized with security changes; a revision conflict preserves local UI edits until you explicitly discard/reload. Required audit commits with course changes. The organization guard is intentionally coarse for this initial monolith.

Use **People** to grant/revoke Course Author on an existing active account. The affected account must sign in again. This workflow neither provisions accounts nor changes privileged memberships or learner seats. Course ownership and the account role stay distinct; [the API contract](../04-api-design/course-authoring.md) documents explicit ownership transfer and target protections. The ordinary bootstrap Administrator can author its own course without needing an additional role.

## Cohort scheduling defaults and limits

Use **People** to establish Cohort Coordinator or Learning Facilitator eligibility, then **Cohorts → New cohort** to select a published course, local display times and initial staff. The API converts schedule values to UTC and creates the cohort plus its Coordinator in one transaction. One account may hold both roles and both distinct grants. Resource assignment never adds the account role. Removing role/access or deactivating the final effective Coordinator of a non-ended cohort is rejected; required audit failure rolls back the change.

The staff view is current-state only. No learner roster, Enrollment, cohort-start email, access-before/after-window rule, withdrawal, transfer or course-archive behavior is implemented. Those belong to later slices and Q69 where applicable. See the [cohort API contract](../04-api-design/cohort-scheduling.md).

## Limits and next work

This slice has no password reset/recovery UI, audit viewer, invitation flow, generic user editor, licensing enforcement, job worker or customer deployment package. There is no path that activates/grants Learner, so zero-seat Administrator setup does not bypass an unimplemented capacity guard. Synthetic Learners exist only in tests.

Expired session rows are denied immediately; automated deletion is not yet scheduled. Before customer release, add bounded cleanup, qualify key rotation/recovery and complete the release/real-data gates. The v1→v2 and v2→v3 development upgrades are covered by integration tests; arbitrary downgrade and production restore are not qualified. Immutable OCI image publication, versioned production Compose packaging, TLS/backup/restore qualification and supported platform commitments remain in [slice 7](../07-roadmap/implementation-plan.md#7--expand-the-mvp-and-qualify-the-customer-release).

## Related documents

[Authoring API](../04-api-design/course-authoring.md) · [Cohort API](../04-api-design/cohort-scheduling.md) · [Identity API](../04-api-design/identity-foundation.md) · [Security principles](security-and-identity.md) · [Release requirements](release-and-operations.md) · [Open questions](../00-product/open-questions.md)
