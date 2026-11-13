---
name: hoc-validate-consumer
description: Diagnose an existing Học-derived consumer with the read-only H4 structural validator and interpret its exact PASS/FAIL output. Use for consumer validation and handoff assessment; do not use for bootstrapping, module generation, repair, migration, deployment, or runtime acceptance.
---

# Validate an Học consumer

Use this skill from a separate Học tooling checkout when a developer wants to
diagnose an existing independent consumer. It runs the existing
`verify-consumer.ps1` and explains what its static evidence means. It does not
rename, repair, generate, compose, migrate, build, start, deploy, or modify the
consumer.

For routing, shared authorization boundaries, and evidence conventions, read
[`docs/productization/agent-skills-v1.md`](../../../docs/productization/agent-skills-v1.md).
Use `hoc-create-consumer` for a new consumer and `hoc-add-admin-module` only
after a consumer exists and the module workflow is explicitly in scope.

## Scope and required inputs

Confirm these values before validation:

- `ConsumerRoot`: the existing consumer directory, resolved to its intended
  path and ownership. Do not silently substitute the tooling checkout or a
  shared/persistent target.
- `ApplicationId`: the consumer identity expected by the solution and source.
  The validator accepts `[A-Za-z][A-Za-z0-9_.-]*`; do not broaden this input or
  infer the identity from branding text.
- `ToolingRoot`: the Học tooling checkout containing the selected validator
  revision.
- `ToolingSha`: the exact tooling revision used for evidence. Record it
  separately from the consumer's source/provenance SHA.
- Expected compatibility: by default SDK `10.0.403`, ABP `10.7.0`, and LeptonX
  Lite `5.7.0`, unless a separately reviewed compatibility baseline is supplied.

Read `AGENTS.md`, `docs/engineering-workflow-v2.md`,
`docs/productization/consumer-bootstrap-v1.md`,
`docs/productization/automated-consumer-bootstrap-v1.md`, and
`docs/productization/compatibility-v1.md` before interpreting results. Confirm
the consumer records its upstream Học repository and source SHA. The source
SHA and tooling SHA are different facts; do not call a later or different
combination compatible without review.

This skill is not for a new consumer (`hoc-create-consumer`), adding a module
(`hoc-add-admin-module`), or fixing a finding. A finding is diagnostic output,
not authorization to edit source, configuration, Identity, grants, or EF
migrations.

## Run the structural validator

Run from the selected tooling checkout with PowerShell 7. The default version
arguments make the supported baseline explicit:

```powershell
Set-Location $toolingRoot
pwsh ./scripts/productization/verify-consumer.ps1 `
  -ConsumerRoot $consumerRoot `
  -ApplicationId $applicationId `
  -ExpectedSdkVersion 10.0.403 `
  -ExpectedAbpVersion 10.7.0 `
  -ExpectedLeptonXVersion 5.7.0
```

Preserve the validator's complete stdout/stderr, each `PASS:` or `FAIL:` line,
the final `RESULT:` line, and the process exit status. Exit `0` with
`RESULT: PASS` is structural evidence only. Exit `1` or any terminating error
is not a pass; retain the exact diagnostic and report the affected category.
Do not suppress failures, change expected versions to make output green, or
automatically rerun after editing the consumer.

The validator is intended to be read-only: it does not require secrets,
network access, database access, migrations, a running host, or a browser. It
does not inspect effective configuration, credentials, permissions, tenant
behavior, or deployed state.

## Interpret only the checks actually performed

Classify the observed output using these categories:

- **Identity and solution references:** expected `<ApplicationId>.slnx`, Web
  project, solution identity, project `RootNamespace`/`AssemblyName`, valid
  solution project paths, project references, and absence of stale `Hoc.*`
  project dependencies. Malformed XML and unresolved references are failures.
- **Stale sample dependencies:** executable/project/configuration scans for
  `Hoc`, `Hoc.*`, `Hoc.DemoRecords`, or `DemoRecords` in inspectable source;
  comments in C# are removed before that scan. Historical documentation is
  excluded from the inspectable scan and is not a failure by itself.
- **Inherited migrations:** files under `Migrations` outside docs, bin, and
  obj whose name or contents identify `Hoc`, `HocDbContext`, or `DemoRecords`.
  The validator does not design, create, apply, or assess the correctness of a
  consumer-owned migration.
- **SDK/package compatibility:** `global.json` SDK version; explicit Volo.Abp
  package versions (excluding the LeptonX package); and the LeptonX Lite
  package version. The validator compares them with the expected parameters;
  it does not restore packages or prove runtime compatibility.
- **Secrets/configuration artifacts:** source-tree `appsettings.secrets.json`
  and `.pfx`, `.p12`, `.pem`, or `.key` files, excluding `.git`, bin, obj,
  node_modules, and docs. Report only relative paths and never print contents.
- **Missing source artifacts:** missing consumer root, expected solution,
  inspectable project files, `global.json`, explicit package versions, project
  files, or referenced solution/project files. Report the exact validator
  diagnostic; do not manufacture a replacement artifact.

The validator also checks for `OpenIddict` source/configuration containing
`<ApplicationId>_Web` and `<ApplicationId>_Swagger`, and flags known remaining
`Hoc` OpenIddict identities. These are static identity checks, not
authentication or authorization evidence.

## Follow-up evidence, separately scoped

After structural validation, optional restore, Release build, or focused tests
must be explicitly selected and reported as separate evidence. They may create
local `bin`/`obj` or package-cache artifacts; preserve the consumer's existing
configuration and do not expose secrets.

```powershell
dotnet restore $solutionPath -m:1 /nr:false
dotnet build $solutionPath -c Release --no-restore -m:1 /nr:false
dotnet test $focusedTestProject -c Release --no-build --no-restore -m:1 /nr:false
```

Use the pinned SDK and only named, focused projects. Missing ignored client
libraries may affect Web/TestServer work; do not turn that into a structural
validator failure or install assets without the appropriate scope.

Keep evidence boundaries explicit:

- structural validator: source/layout/package-artifact checks only;
- compilation: compiler/build evidence only;
- unit tests: test-project evidence only;
- SQLite/TestServer: isolated test-host evidence only;
- PostgreSQL: database/provider evidence, requiring its own target and scope;
- browser: actual browser evidence with DOM/network/console observations;
- production/deployed runtime: separate operational evidence.

None of the first four categories proves the latter three. Never run
`DbMigrator`, seed, apply migrations, write a persistent database, change
Identity or grants, deploy, or repair a finding as part of this diagnostic.

## Report format and next actions

Return a concise report containing:

1. Consumer root, `ApplicationId`, consumer provenance/source SHA, tooling
   revision, expected versions, command, and exit status.
2. Exact validator `PASS:` and `FAIL:` diagnostics grouped by the categories
   above, including the final `RESULT:` line.
3. Facts established, failures requiring owner action, and unknowns that this
   static validator cannot establish.
4. Recommended next action for each failure: inspect, obtain a separately
   scoped source/configuration correction, review compatibility, or run a
   separately authorized restore/build/test. Do not imply that a recommendation
   authorizes the change.

If the validator cannot run, report `NOT VERIFIED` with the command and
sanitized error. Do not claim structural PASS from a missing consumer, a
different tooling revision, compilation, H4 self-tests, or historical evidence.
