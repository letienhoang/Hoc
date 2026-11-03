# Học repository guidance

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

Use `./.tools/dotnet/dotnet.exe` selected by `global.json`. On Windows, shared
generated outputs can lock; use serialized builds (`-m:1`, and `/nr:false` if
needed) before classifying `MSB4018` or `MSB3713` as source defects. Run only
the focused, authorized validation first. Distinguish SQLite/TestServer/local
evidence from PostgreSQL, browser, deployed-runtime, and production proof.

## EF Core and migrations

Changes to entities, mappings, indexes, or persistence need explicit migration
review. Use `Hoc.EntityFrameworkCore` as both EF tooling project and startup
project because it owns `HocDbContextFactory`. `Hoc.DbMigrator` is write-capable
(migration and seed); never run it or connect to a database without an approved
disposable target and Owner authorization.

## Security and publication

Fail closed on missing identity, target ownership, credentials, or approval.
Never print, store, or stage secrets. ABP-derived source and template assets
retain their LGPL-3.0-only provenance; Apache-2.0 applies only to Học-owned
material where copyright authority exists. LeptonX Lite remains an external
NuGet dependency: do not vendor, extract, or claim redistribution rights.

## Codex working rules

Check the exact worktree, branch/HEAD, status, and allowed paths before edits.
Preserve unrelated files. Use named-path staging only when explicitly
authorized; this repository currently has an Owner NO-GO for repository
creation, staging, commits, pushes, packages, and deployment. For
documentation-only changes, inspect the exact diff and run `git diff --check`;
state omitted build/test work and all unresolved gates.
