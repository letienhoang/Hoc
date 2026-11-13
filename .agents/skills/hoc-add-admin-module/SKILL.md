---
name: hoc-add-admin-module
description: Generate and integrate the supported fixed-shape ABP MVC/Razor CRUD module into an existing independent Học consumer. Use for the H4-02 module starter followed by H4-04 host composition; do not use for creating a consumer, arbitrary entity design, migrations, deployment, or upgrades.
---

# Add an Học admin module

Use this skill from the Học tooling checkout when a developer has an existing
independent consumer and wants the supported six-project CRUD module generated
and composed into that consumer. It orchestrates the existing H4-02 and H4-04
scripts; it is not a new generator, installer, runtime plugin system, or
migration tool.

## Read before acting

Read these files at the current tooling revision:

- `AGENTS.md` and `docs/engineering-workflow-v2.md` for authorization and
  repository safety rules.
- `docs/productization/module-starter-v1.md` for the fixed module contract and
  ownership boundary.
- `docs/productization/automated-module-integration-v1.md` for composition
  behavior and idempotence.
- `docs/productization/compatibility-v1.md` for source, dependency, and
  provenance limits.
- `scripts/productization/new-admin-module.ps1` and
  `scripts/productization/integrate-admin-module.ps1` for the executable
  parameter contracts. Do not infer a newer or broader contract.

## Required inputs and preflight

Confirm all five values with the developer before running either script:

- `ConsumerRoot`: an existing, consumer-owned directory, not the Học tooling
  checkout, not a reparse point, and not a shared or persistent target unless
  that target is explicitly in scope.
- `ApplicationId`: a PascalCase C# identifier matching
  `^[A-Z][A-Za-z0-9]*$`.
- `ModuleName`: a PascalCase module identity matching the same pattern.
- `EntityName`: a PascalCase entity identity matching the same pattern.
- `RouteSegment`: a lowercase route containing only letters, digits, and
  hyphens, matching `^[a-z][a-z0-9-]*$`.

Inspect the consumer root and confirm:

1. `<ApplicationId>.slnx` and `common.props` exist. The H4-02 generator also
   accepts a legacy `.sln`, but H4-04 integration requires the `.slnx`; stop
   rather than silently substituting another solution.
2. The consumer is an independent derived source tree and the requested
   module is not already present.
3. None of the six H4-02 output directories exists:
   `modules/<ApplicationId>.<ModuleName>.{Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi,Web}`.
4. The requested identity does not collide with an existing module or consumer
   composition. Preserve unrelated worktree changes.

The fixed v1 aggregate supports only a `Guid` ID, required `Name` (128), and
optional `Description` (1024). Do not promise arbitrary fields, relationships,
host editing, runtime loading, package publishing, or automatic upgrades.

## Stage 1: H4-02 generation dry-run

Run from the tooling checkout with PowerShell 7 and no `-Apply`:

```powershell
pwsh ./scripts/productization/new-admin-module.ps1 `
  -ConsumerRoot $consumerRoot `
  -ApplicationId $applicationId `
  -ModuleName $moduleName `
  -EntityName $entityName `
  -RouteSegment $routeSegment
```

Review the complete plan and require exit code `0` plus
`Dry-run complete; no consumer files were changed.` before any write. A
collision, invalid identifier, missing solution/common props, unsafe path, or
template-baseline failure is a generator precondition failure. Do not bypass it
or delete/overwrite existing consumer output.

Running H4-02 with `-Apply` is a bounded filesystem mutation. Require explicit
authorization for this exact `ConsumerRoot` and identity before proceeding:

```powershell
pwsh ./scripts/productization/new-admin-module.ps1 `
  -ConsumerRoot $consumerRoot `
  -ApplicationId $applicationId `
  -ModuleName $moduleName `
  -EntityName $entityName `
  -RouteSegment $routeSegment `
  -Apply
```

After a successful apply, expect these six consumer-owned projects:

```text
modules/<ApplicationId>.<ModuleName>.Domain/
modules/<ApplicationId>.<ModuleName>.Application.Contracts/
modules/<ApplicationId>.<ModuleName>.Application/
modules/<ApplicationId>.<ModuleName>.EntityFrameworkCore/
modules/<ApplicationId>.<ModuleName>.HttpApi/
modules/<ApplicationId>.<ModuleName>.Web/
```

## Stage 2: H4-04 integration dry-run and apply

Only after successful generation, verify the six project files and expected
module entry points exist, then run the integrator dry-run:

```powershell
pwsh ./scripts/productization/integrate-admin-module.ps1 `
  -ConsumerRoot $consumerRoot `
  -ApplicationId $applicationId `
  -ModuleName $moduleName `
  -EntityName $entityName
```

Review the plan and require exit code `0` before a second, separately bounded
authorization for integration. H4-04 updates exactly six consumer composition
files: `<ApplicationId>.slnx`, the Web `.csproj` and Web module, the EF Core
`.csproj` and EF Core module, and the consumer `DbContext`. It adds the module
projects, host/module dependencies, the entity `DbSet`, and the
`Configure<ModuleName>()` model call.

With that exact authorization, apply integration:

```powershell
pwsh ./scripts/productization/integrate-admin-module.ps1 `
  -ConsumerRoot $consumerRoot `
  -ApplicationId $applicationId `
  -ModuleName $moduleName `
  -EntityName $entityName `
  -Apply
```

Do not confuse the two failure classes:

- H4-02 output collisions are generator failures. Stop and preserve the
  existing module; do not force or overwrite it.
- H4-04 is designed to be idempotent after a complete integration. A dry-run
  or apply that reports `Consumer is fully integrated; no changes are
  required.` is a successful no-op. Ambiguous or partially inconsistent
  composition fails closed and requires inspection, not repeated apply.

## Verification and handoff

Perform only focused, non-persistent verification proportional to the change:

- Recheck the six project paths, six composition targets, project references,
  module dependencies, `DbSet`, and `Configure<ModuleName>()` call.
- Use the consumer's pinned SDK for serialized restore and Release build when
  authorized: `dotnet restore <ApplicationId>.slnx -m:1 /nr:false`, followed by
  `dotnet build <ApplicationId>.slnx -c Release --no-restore -m:1 /nr:false`.
- Existing H4 self-tests may be used for tool-focused validation; do not rerun
  the full H4 acceptance suites merely to claim consumer acceptance.

Report the tooling revision, consumer root and consumer revision, all five
identities, each dry-run/apply result, generated project paths, six composition
targets, verification commands and actual exit codes. Mark unavailable runtime,
disposable-consumer, browser, PostgreSQL, or production evidence as
`NOT VERIFIED`; do not infer it from source inspection or a build.

Generation and integration do not create or apply EF migrations. The consumer
owns migration **source** design and review, including `Up`, `Down`, and the
model snapshot. Migration APPLY, `DbMigrator`, seeding, persistent database
writes, Identity/grant changes, deployment, and production operations are
outside this skill and require separate authorization and evidence. Preserve
license/provenance notices and do not vendor or claim redistribution rights for
LeptonX Lite.
