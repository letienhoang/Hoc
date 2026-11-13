---
name: hoc-create-consumer
description: Bootstrap a new independent Học ABP MVC/Razor Admin consumer using the existing H4-03 source-based generator. Use when a developer asks to create, scaffold, or derive a new Admin application from Học. Do not use for adding modules to an existing consumer, database migrations, deployment, or automatic upgrades.
---

# Create a Học consumer

Use this skill from the `letienhoang/Hoc` tooling checkout. It orchestrates the existing H4 scripts; it does **not** replace their validation or create a database, remote repository, or running website.

For routing, shared authorization boundaries, and evidence conventions, read
[`docs/productization/agent-skills-v1.md`](../../../docs/productization/agent-skills-v1.md).
After a successful consumer bootstrap and its focused verification, module
addition is a separate handoff to `hoc-add-admin-module`; use
`hoc-validate-consumer` when the starting point is an existing consumer that
needs diagnosis rather than creation.

## Authoritative references

Read these files at the current tooling revision before executing commands:

- `AGENTS.md` and `docs/engineering-workflow-v2.md` — repository authorization and safety rules.
- `docs/productization/automated-consumer-bootstrap-v1.md` — exact generator contract.
- `docs/productization/compatibility-v1.md` — compatibility and ownership limits.
- `scripts/productization/new-consumer.ps1` and `scripts/productization/verify-consumer.ps1` — actual executable interfaces.

Do not assume that a newer Học `main` revision is a supported consumer source. H4-03 v1 supports only source SHA `6a8605a63d18d35767f520d57d33193f6d15aa29`, with .NET SDK `10.0.403` and ABP `10.7.0`. Keep the tooling revision and consumer source SHA distinct.

## Collect and validate inputs

Obtain or confirm these exact values from the developer before proceeding:

- `SourceRoot`: an existing local checkout of `letienhoang/Hoc` containing the pinned source commit (not necessarily the current tooling checkout).
- `SourceSha`: the supported full SHA above.
- `OutputRoot`: a **new, nonexistent** directory whose parent exists, outside `SourceRoot` and outside any existing Git worktree.
- `ApplicationId`: a PascalCase C# identifier (for example, `InventoryAdmin`).
- `RootUrl`: an absolute HTTPS origin with no path, query, or fragment (for example, `https://localhost:44320`).

Confirm intended destination and identity; do not silently invent a filesystem path, external domain, or application identity. Inspect current tooling Git root/HEAD/status and the source checkout origin/commit availability. Keep unrelated worktree changes untouched. Let the generator enforce its full path, reparse-point, origin, SDK, and identity checks; never bypass them on validation failure.

## Execute in two separate stages

Run from the Học **tooling** checkout using PowerShell 7 (`pwsh`). Set the variables to the developer-approved values; the following are synthetic examples:

~~~powershell
$sourceRoot = 'C:\work\Hoc-source'
$outputRoot = 'C:\work\InventoryAdmin'
$applicationId = 'InventoryAdmin'
$rootUrl = 'https://localhost:44320'
$sourceSha = '6a8605a63d18d35767f520d57d33193f6d15aa29'

$bootstrapArgs = @(
  '-SourceRoot', $sourceRoot,
  '-SourceSha', $sourceSha,
  '-OutputRoot', $outputRoot,
  '-ApplicationId', $applicationId,
  '-RootUrl', $rootUrl
)

# Stage 1 — non-mutating inspection (default; no -Apply).
pwsh ./scripts/productization/new-consumer.ps1 @bootstrapArgs
~~~

Inspect the complete dry-run result and resolve errors before any write. **Stage 2 is a filesystem write**: run `-Apply` only when the developer/Owner has explicitly authorized creating this consumer at the exact `OutputRoot`. A previously granted, bounded authorization covering that destination is sufficient; do not ask repeatedly. If authorization is absent, stop after the dry-run and report what is pending.

~~~powershell
# Stage 2 — only after the successful dry-run and scoped authorization.
pwsh ./scripts/productization/new-consumer.ps1 @bootstrapArgs -Apply

# Static, read-only verification of generated source structure.
pwsh ./scripts/productization/verify-consumer.ps1 -ConsumerRoot $outputRoot -ApplicationId $applicationId
~~~

If generation or validation fails, report the command's sanitized error and actual on-disk state. Do not force, overwrite, delete, rename, or manually patch generated output to circumvent a guard. Follow the canonical docs for any separately authorized recovery.

## Completion and handoff

Report the tooling HEAD, pinned source SHA, consumer path and application ID, dry-run outcome, whether `-Apply` was authorized/executed, verification result, and any remaining steps. Explain that the generated consumer owns its projects, configuration, OpenIddict identities, DbContext, migration source, modules, tests, branding, and deployment. It has no automatic upstream update channel or binary compatibility guarantee.

The validator checks structure only. Do **not** claim that successful generation or Release compilation proves PostgreSQL, authentication, browser, tenant security, or production readiness. Do not automatically generate/apply migrations, run `DbMigrator`, seed data, modify Identity/permissions, access persistent databases, create a remote repository, publish, or deploy. Those actions require their own scope and authorization. Module creation/integration is a separate workflow.

Repository-scoped skills are discovered while working inside the Học checkout; H4-03's pinned-source consumer does **not** automatically inherit this skill.
