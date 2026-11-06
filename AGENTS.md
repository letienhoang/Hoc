# Học repository guidance

The canonical workflow and authorization matrix is
[docs/engineering-workflow-v2.md](docs/engineering-workflow-v2.md).
Owner approved this process on 2026-11-06. It becomes repository policy
in `main`; any task-specific Owner restriction overrides its general
allowances.

## Architecture

This is an ABP 10 layered MVC modular monolith. Keep the dependency direction:
`Domain.Shared` -> `Domain` -> `Application.Contracts` -> `Application`;
the EF Core, HTTP API, Web, and DbMigrator projects are adapters/hosts. Do not
add modules, change architecture, or bypass ABP authorization, validation, or
unit-of-work boundaries without an accepted ADR and Owner approval.

## Code and configuration

Follow the surrounding ABP and .NET conventions: nullable enabled, async I/O,
`CancellationToken` where the surrounding API supports it, and no secret or
machine-specific value in tracked source. Keep runtime configuration in the
ignored `appsettings.secrets.json` overlay or approved external configuration.
Do not commit `.tools/`, logs, restored client libraries, certificates/keys,
local database state, environment overlays, user settings, or publish output.

## Build and tests

Respect `global.json` and the repository's pinned .NET SDK. For local Windows
development, use `./.tools/dotnet/dotnet.exe` when present; GitHub Actions uses
`actions/setup-dotnet` to install the same SDK. Shared generated outputs can
lock on Windows; use serialized builds (`-m:1`, and `/nr:false` if needed)
before classifying `MSB4018` or `MSB3713` as source defects.

Run focused local tests first. The PR CI supplies clean Release build and
relevant full test suites. Distinguish locally reported, CI, SQLite/TestServer,
PostgreSQL, browser and deployed-runtime evidence. CI must not require real
Neon secrets or start DbMigrator.

For browser acceptance, prefer Codex in Chrome (`@Chrome`) and retain
DOM, network and console evidence. If Chrome connectivity is blocked, report
that limitation; do not automatically switch to custom CDP, HeadlessChrome or
desktop automation.

## EF Core and migrations

Changes to entities, mappings, indexes, or persistence need explicit migration
review. Use `Hoc.EntityFrameworkCore` as both EF tooling project and startup
project because it owns `HocDbContextFactory`. `Hoc.DbMigrator` is write-capable
(migration and seed): **never execute it on a persistent database without a
separate, bounded Owner GO**, even after a code PR is approved or merged.

Read-only queries on an Owner-designated development target are allowed only
within an authorized task scope. Verify the effective database and TLS; do not
re-demand Owner evidence without reason. Production/destructive changes always
need operation-specific approval and a recovery plan. For current Neon first
initialization, use explicit Development + HostOnly mode and verify `hoc_dev`
before any separately authorized write.

## Security and publication

Fail closed on missing identity, target ownership, credentials or approval.
Never print, store, or stage secrets. ABP-derived source and template assets
retain their LGPL-3.0-only provenance; Apache-2.0 applies only to Học-owned
material where copyright authority exists. LeptonX Lite remains an external
NuGet dependency: do not vendor, extract, or claim redistribution rights.

## Codex working rules — scope-based authorization

Check exact worktree, branch/HEAD, status, and allowed paths before edits.
Preserve unrelated files. **One explicit Owner GO for a bounded implementation
task authorizes** a feature branch, scoped edits, non-mutating build/tests,
named-path staging, commit, push and Draft PR creation/update within that
task's allowed paths. Do not stop merely to re-ask permission for each Git step.
Before committing, inspect the staged diff and verify no secret/generated or
out-of-scope content. If the task's objective or paths change, stop for renewed
scope authorization.

This replaces the historical blanket NO-GO for staging, commit, push and PR
publication. It does **not** authorize new repository creation, merging,
deploying, running DbMigrator, writing persistent DB data, or modifying
production. Owner controls merges during initial rollout. High-risk changes
(authentication, authorization, secrets, migrations, tenant boundaries)
require TL review before Owner merge decision. Document omitted checks and
remaining gates without repeatedly requesting proof already established.
