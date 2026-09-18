# Release distribution and operations

> **Status:** Confirmed baseline; release-specific support matrix pending\
> **Authority:** Canonical — ADR-2, ADR-5 and ADR-10\
> **Updated:** 2026-09-17

## Purpose

Ship Variable LMS as a customer-operated product independently of whether its backend is one process or, later, several services.

## Release identity and packages

Variable publishes OCI images to a registry with immutable release-tag policy. The registry provider is an engineering choice. Use semantic application versions (`MAJOR.MINOR.PATCH`, with explicit prerelease labels before a stable release), meaningful release notes and immutable release artifacts. Never overwrite a released tag; publish a new patch. Record/pin image digests in the deployment package so mutable registry configuration cannot silently change a release.

A versioned Compose package contains the deployment description, configuration reference, image/version/digest bill of materials, package checksum, installation/upgrade steps and release compatibility manifest. Customers pull the images; no source build or interactive Variable server access is required for delivery. Separate local secrets/operator overrides from vendor-managed package files so upgrading does not overwrite customer configuration.

Helm is the intended versioned Kubernetes package when supported; do not require hand-maintained large raw YAML sets. Its package version is separate from application image versions. `appVersion` alone is informational, not a compatibility guarantee ([Helm chart documentation](https://helm.sh/docs/topics/charts/)). The first implementation targets Compose; [Q63](../00-product/open-questions.md#q63) gates which deployment paths are promised at first external release. Kubernetes is never required for a small installation.

Every published package must declare:

- Package version and exact supported application images/digests; initially the backend and built SPA ship together.
- Supported database/runtime/platform versions and architectures, persistent volumes, required integrations, and resource assumptions.
- Current/target schema version, supported upgrade source versions, any mandatory intermediate release, and migration prerequisites.
- License-protocol compatibility and applicable configuration changes/deprecations.
- Whether application rollback on the migrated schema is tested, or restore is required.

A package-only correction gets a new package version without rebuilding or mutating an unchanged application image. An application SemVer major does not itself prove a database upgrade path; the release manifest must state tested compatibility. Future service extraction adds independently versioned images to that same manifest model.

## Customer upgrade control

Customers own installation and upgrade timing. Release feeds, vendor repositories and GitOps are optional distribution conveniences. Following a release source must not imply automatic application. No application auto-updater or silent database upgrade runs on a timer. Future managed hosting has separate operational/tenancy/provisioning requirements; the same image is deployable there but is not evidence those capabilities exist now.

## Upgrade and recovery

Baseline packaging uses an explicit maintenance/upgrade phase:

1. Read release notes and validate the supported source version, package/image digests, configuration, free space and external dependencies.
2. Quiesce web writes and workers; take a recoverable backup/checkpoint of database **and** matching files/configuration/key material. Verify the restore procedure has been exercised. Record old image/package/schema identity.
3. Pull the selected release artifacts without replacing customer secrets. Run the packaged migration command with migration-only credentials and a database lock/ledger. Module migrations execute in an ordered, reviewed sequence.
4. Start the normal runtime with its restricted identity; check schema compatibility, readiness, login and a learning-data smoke check before restoring traffic/workers.
5. On failure, keep the installation unavailable for writes, retain diagnostic/migration state and follow the release's recovery path. Do not start a version against an unsupported schema.

Never edit a shipped migration. Use transactional migrations where supported; explicitly document recovery for nontransactional operations. A lock prevents simultaneous migrators, not unsafe concurrent old-version traffic. Keep migrations additive/backward compatible where practical, and delay destructive contraction until the advertised compatibility window permits it.

Application-only rollback is allowed **only** when the release declares the old application compatible with the current schema. Otherwise restore the matching pre-upgrade database/files/keys and old images. Do not promise automatic down-migration or zero data loss after accepting new writes. Backup duration, tolerated downtime, recovery point/time and first supported upgrade paths must be measured before release ([Q61](../00-product/open-questions.md#q61)).

## Storage and backup

The minimum installation is the application plus PostgreSQL, persistent storage and configured SMTP; no mandatory Redis, S3, broker or Kubernetes. Use a persistent local volume for the limited MVP files initially. Add an object-storage implementation when deployment needs it; keep provider-specific details out of domain records. A storage key denotes owned content, not a public URL.

Store organization logo/branding assets and any retained certificate render artifacts outside ephemeral container layers. Certificate rendering must use frozen display data, template/version and stable branding assets; never fetch arbitrary remote URLs during rendering. A simple PDF can render on demand; only introduce PDF jobs if measured cost justifies them. The user-visible readiness/download behavior must still meet the agreed slice acceptance.

Backups cover PostgreSQL (including sessions/jobs/audit), referenced logo/PDF files, protected data-protection keys, license file/public-key trust configuration, installation configuration and the artifact/version manifest. Store/recover secrets securely and separately where appropriate. A `pg_dump` alone does not cover these files. Test restoration into an isolated installation; invalidate restored sessions and rotate recovery-sensitive credentials as needed before exposure. Do not replay historical notification delivery blindly after restore.

## Configuration and environments

Use one image configured at runtime for local, test, staging and production installations. Document database runtime and separate migration connections; organization identity; public base URL and trusted proxies; storage root/provider; SMTP; license path/trust keys; data-protection location; log level; health/metrics exposure. Validate required configuration at startup and avoid logging values of secrets. Mount secret files or inject secrets through the customer's secret mechanism; no hardcoded credentials.

Developer/test fixtures use clearly non-production organizations, credentials and license keys. Test signing keys never authorize production licenses. Local mail capture is a development tool, not a required product integration. Offline licensing means no live Variable control-plane dependency; SMTP may be internal, and external reading URLs still require their own reachability. Full air-gap usability must account for those dependencies and locally supplied static assets/fonts.

## Observability and support

Structured logs with correlation IDs; liveness for process responsiveness; readiness for compatible schema, usable configuration, and required persistence. Expose optional metrics behind operator-controlled access: request latency/errors, DB contention, work backlog/retry age, licensing denial counts and certificate failures. Do not label metrics with learner names/emails or high-cardinality personal data. SMTP delivery failure appears in operational diagnostics/retry state and need not make the whole LMS unready.

Provide documented inspection/retry commands or administrative operations for failed durable work when implementing that slice. Audit user/role/license/reset/certificate/moderation changes. Keep operational logs distinct from the authoritative audit record. CI builds/tests and produces artifacts; release publishing/deployment is an explicitly authorized workflow. No pipeline or deploy package is implemented in this documentation pass.

## Open questions

[Q63](../00-product/open-questions.md#q63): launch deployment support. [Q61](../00-product/open-questions.md#q61): measured workload/recovery targets. [Q68](../00-product/open-questions.md#q68): license failure behavior. [Q78](../00-product/open-questions.md#q78): external distribution terms.

## Related documents

[ADRs](decisions.md) · [Security](security-and-identity.md) · [License contract](../04-api-design/license-contract.md) · [Implementation plan](../07-roadmap/implementation-plan.md)
