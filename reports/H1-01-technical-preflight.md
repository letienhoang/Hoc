# H1-01 Technical Preflight and Local Host Bootstrap

**Executed:** 2026-11-02 (Asia/Saigon)
**Status:** BLOCKED before scaffolding; no existing application, remote, VPS, or database was changed.

## Scope and evidence boundary

The requested work is confined to `D:\job\OaiPhongLabs\HocTat\Hoc`. This checkout is an unborn Git `main` branch: it has no commit, no tracked files, no configured remote, and no project, solution, ADR, or `AGENTS.md` file. A recursive search of `D:\job\OaiPhongLabs\HocTat` found no `ADR-0001` or `H1-01` source.

The owner authorization permits this H1-01 execution, but does not replace the missing Owner-accepted ADR-0001. Selecting an ABP template, solution name, module set, database schema/role layout, or configuration contract without that ADR would silently make architectural decisions. This report therefore records verified preflight facts only.

## Commands actually run

```powershell
Get-ChildItem -Path . -Filter AGENTS.md -Recurse -Force
git status --short
git branch --show-current
git rev-parse HEAD
Get-ChildItem -Force

dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
abp --version
docker --version
docker compose version
psql --version
git log -1 --oneline

git remote -v
git config --get remote.origin.url
Get-ChildItem -Path .. -Filter 'ADR-0001*' -Recurse -File
rg -l -i --glob '!*bin*' --glob '!*obj*' 'ADR-0001|H1-01|LeptonX Lite|ABP 10\\.7' 'D:\job\OaiPhongLabs\HocTat'

docker context show
docker info --format '{{.ServerVersion}}|{{.OperatingSystem}}'
docker compose ls
```

No command that creates a repository, contacts GitHub, starts a container, changes a database, installs a tool, or writes outside this workspace was run.

## PASS (verified)

| Area | Evidence | Result |
|---|---|---|
| ABP / .NET compatibility | ABP 10.7 Final was released 2026-10-07. `Volo.Abp.AspNetCore.Mvc` 10.7.0 targets `net10.0`; local `Microsoft.AspNetCore.App` and `Microsoft.NETCore.App` 10.0.9 runtimes are installed. | PASS — framework/runtime compatibility is supported. |
| Free-theme policy | ABP documents LeptonX Lite as the official/default free theme. It is the first theme to try; Basic Theme is the free minimalist fallback. | PASS — no commercial LeptonX package is needed for H1-01. |
| Theme package availability | NuGet lists `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` 5.7.0 targeting `net10.0`. Its release version intentionally differs from the ABP Framework 10.7.0 version. | PASS — compatible candidate identified, not yet restored. |
| License boundary | The ABP framework and MVC packages are published as LGPL-3.0-only; LeptonX Lite is documented as the free theme; LeptonX (without Lite), ABP Suite, and commercial modules are excluded. | PASS — proposed dependency boundary contains no commercial ABP dependency. Compliance review must preserve required notices when packages are actually restored/distributed. |
| Workspace isolation | The workspace contains only `.git`; no remote is configured. | PASS — no existing app can be altered by this execution. |
| Secret handling | No connection strings, passwords, tokens, or credentials were read, created, printed, or stored. | PASS. |

Official sources consulted:

- [ABP 10.7 Final announcement](https://abp.io/community/announcements/announcing-abp-10-7-stable-release-7sv0w8hp)
- [ABP 10.0 migration guide (.NET 10)](https://abp.io/docs/latest/release-info/migration-guides/abp-10-0?LanguageCode=en)
- [Volo.Abp.AspNetCore.Mvc 10.7.0 NuGet metadata](https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc/)
- [LeptonX Lite documentation](https://abp.io/docs/latest/ui-themes/lepton-x-lite/)
- [ABP official themes](https://abp.io/docs/latest/ui-themes?LanguageCode=en)
- [LeptonX Lite 5.7.0 NuGet metadata](https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite/)

## UNRESOLVED

| Item | Why it remains unresolved |
|---|---|
| ABP template and project structure | `ADR-0001` is not present or reachable from this workspace/parent tree. The intended template, app/assembly name, layering, modules, and whether the host needs a separate migrator cannot be verified. |
| Template dependency and asset inventory | There is no generated template or restored `packages.lock.json` / `project.assets.json` to inspect. The items above are candidate metadata only, not a resolved dependency graph. |
| LeptonX Lite rendering | The requested first attempt cannot be made until a host is selected and scaffolded. Basic Theme was not attempted, because there is no LeptonX Lite failure to justify a fallback. |
| Publication-safety inspection | There are no generated `appsettings*`, launch profiles, Docker files, static assets, or publish output to inspect. |

## BLOCKED

| Blocker | Concrete evidence | Effect |
|---|---|---|
| ADR-0001 unavailable | Empty checkout; no `ADR-0001`/`H1-01` source found in the workspace or parent tree. | Blocks authorized template selection and all host scaffolding. |
| Stable SDK absent | The only installed SDK is `10.0.400-preview.0.26322.102`; `dotnet --info` labels it preview. Runtime 10.0.9 is stable, but an SDK is required for reproducible restore/build. | Blocks a stable build/restore claim. |
| ABP CLI absent | `abp --version` reports command not found. | Blocks CLI-template creation until a workspace-local approved CLI is installed. |
| Disposable PostgreSQL host unavailable | `psql` is absent. Docker 29.8.2 / Compose 5.5.1 CLIs exist, but `docker info` and `docker compose ls` fail because `//./pipe/docker_engine` does not exist; Docker also cannot read `C:\Users\Administrator\.docker\config.json`. | Blocks safe local PostgreSQL container creation and database-backed runtime verification. |

## Current project structure and modified files

```text
Hoc/
├── .git/                 # unborn main branch, no commit
└── reports/
    └── H1-01-technical-preflight.md
```

Modified file: this report only. No ABP host, database definition, credentials, migrations, content records, theme override, or production-related file exists or was created.

## Required input and safe next H1 action

Technical Lead review is required before H1-02. To resume H1-01 without altering ADR-0001, provide the accepted ADR-0001 in this workspace (or its exact approved location) and make a stable .NET 10 SDK available. Then, with Docker Desktop/engine running or an explicitly approved alternative disposable local PostgreSQL host, execute these bounded steps:

1. Install/pin the ABP CLI **inside this workspace** at the ADR-approved 10.7 version; do not install commercial tooling.
2. Scaffold precisely the ADR-selected MVC/Razor thin host in this isolated checkout.
3. Restore with a lock file, inspect the full resolved package/license and static-asset inventory, and try LeptonX Lite before considering Basic Theme.
4. Use only a secret-excluded local configuration mechanism for a disposable PostgreSQL instance; do not commit a password or connection string containing credentials.
5. Run restore, Release build, a bounded local startup smoke, and a visual/theme rendering check. Inspect publish output and all generated configuration before reporting a runnable host.

H1-02 has not been started.

---

## H1-01B Build Verification & Dependency Security Review — 2026-11-02

**Scope:** local `D:\job\OaiPhongLabs\HocTat\Hoc` only. The Git repository remains an unborn `main` branch with no `HEAD`, no configured remote, and all scaffold files untracked. No repository was created, committed, or pushed. Docker, the existing PostgreSQL Windows service, application databases, and system/global environment configuration were not touched.

### Build result

The local executable selected by `global.json` was verified before every command:

```text
> .\.tools\dotnet\dotnet.exe --version
10.0.403
```

The first normal-parallel Release build compiled all projects but failed once during ASP.NET Core test-manifest generation:

```text
Microsoft.AspNetCore.Mvc.Testing.targets(38,5): error MSB4018:
GenerateMvcTestManifestTask failed because ... Hoc.Web.Tests\obj\Release\net10.0\MvcTestingAppManifest.json ... is being used by another process.
```

This was a local output-file lock, not a C# or generated configuration defect. The same solution built cleanly when serialized, without a source workaround:

```text
> .\.tools\dotnet\dotnet.exe build .\Hoc.slnx --no-restore --configuration Release --nologo --verbosity quiet -m:1
Build succeeded.
    3 Warning(s)
    0 Error(s)
```

Those three warnings were kept separate and not suppressed: generated `CS0162` in `HocMenuContributor.cs`, generated test-fixture `CS8618` for `_database`, and `CS7022` from `Microsoft.NET.Test.Sdk` generated source. After the configuration-only secret-overlay adjustment below, the incremental serialized build reported `0 Warning(s), 0 Error(s)`.

### Dependency audit and remediation

The SDK 10 syntax is `dotnet package list --project <project> --vulnerable --include-transitive`; it accepts a project, not a positional `.slnx`. A complete pre-remediation solution audit used the compatible solution form:

```text
> .\.tools\dotnet\dotnet.exe list .\Hoc.slnx package --vulnerable --include-transitive --format json --output-version 1 --no-restore
```

It found the same two transitive packages at `10.0.11` in eight lock files: Application, Domain, EntityFrameworkCore, DbMigrator, Application.Tests, Domain.Tests, EntityFrameworkCore.Tests, and Web.Tests. The resolved paths are rooted in generated ABP Identity dependencies, for example:

```text
Volo.Abp.Identity.Application
  -> Volo.Abp.Identity.Domain
  -> Microsoft.AspNetCore.DataProtection
  -> System.Security.Cryptography.Xml
  -> System.Security.Cryptography.Pkcs
```

The Domain and Entity Framework roots are `Volo.Abp.Identity.Domain` and `Volo.Abp.Identity.EntityFrameworkCore`; `Volo.Abp.OpenIddict.EntityFrameworkCore` also reaches Identity Domain. DbMigrator and the affected test projects inherit those generated application/test graphs. ABP remains at `10.7.0`; Npgsql remains `10.0.3`; LeptonX Lite remains `5.7.0`.

| Package | Resolved vulnerable version | Advisory IDs / severity | .NET 10 affected range | Microsoft fixed floor |
|---|---:|---|---|---:|
| `System.Security.Cryptography.Pkcs` | 10.0.11 | GHSA-2fmv-rv2c-59x7 / CVE-2026-70162 — Moderate; GHSA-846f-f764-76mx / CVE-2026-84008 — High | 10.0.0–10.0.12 | 10.0.13 |
| `System.Security.Cryptography.Xml` | 10.0.11 | GHSA-7hqp-7c5m-4vq6 / CVE-2026-96960 — High; GHSA-c399-9853-6jmj / CVE-2026-96959 — High; GHSA-r955-8q5j-3h83 / CVE-2026-96958 — High | 10.0.0–10.0.12 | 10.0.13 |

Official Microsoft advisories: [CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), [CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx), [CVE-2026-96960](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6), [CVE-2026-96959](https://github.com/advisories/GHSA-c399-9853-6jmj), and [CVE-2026-96958](https://github.com/advisories/GHSA-r955-8q5j-3h83). Each identifies `10.0.13` as the first fixed .NET 10 package version.

The minimal compatible remediation adds `System.Security.Cryptography.Xml` `10.0.14` as a private direct asset only to the eight affected project graphs. NuGet's `10.0.14` package metadata declares an exact net10 dependency on `System.Security.Cryptography.Pkcs` `10.0.14`; no ABP package was upgraded. `restore --use-lock-file --force-evaluate` refreshed the lock files without NU190x output. Each affected lock now resolves:

```text
System.Security.Cryptography.Xml  Direct@10.0.14
System.Security.Cryptography.Pkcs Transitive@10.0.14
```

`VULNERABLE_10_0_11=0` across all eight affected lock files. A new CLI audit of `src\Hoc.Application\Hoc.Application.csproj` returned an empty vulnerable package list with exit code 0. This is remediation evidence for the previously reported advisories; it is not a claim that no future advisories can exist.

### Test evidence

```text
> .\.tools\dotnet\dotnet.exe test .\test\Hoc.Web.Tests\Hoc.Web.Tests.csproj --no-restore --no-build --configuration Release
Passed: 1; Failed: 0; Skipped: 0; Total: 1

> .\.tools\dotnet\dotnet.exe test .\test\Hoc.EntityFrameworkCore.Tests\Hoc.EntityFrameworkCore.Tests.csproj --no-restore --no-build --configuration Release
Passed: 3; Failed: 0; Skipped: 0; Total: 3
```

The EF Core test module creates an in-memory SQLite database. These four passing tests validate the build/remediation in their local test harness only; they do not start PostgreSQL, apply Npgsql migrations, or establish host runtime readiness. Generated abstract Application/Domain base test assemblies report no discoverable tests by design.

### Configuration, publication, and database readiness

- `.tools/`, `**/appsettings.secrets.json`, and `*.pfx` are now ignored. `git check-ignore` verified the local SDK and each generated secret-overlay path are excluded; existing `bin/` and `obj/` rules remain in effect.
- `HocDbContextFactory` now optionally layers the ignored `Hoc.DbMigrator/appsettings.secrets.json` over tracked settings, matching the Web host and DbMigrator host convention. **EF model changed: no. Migration required/generated: no. Database application: no.**
- Generated tracked configuration still contains template-only development placeholders and generated authentication defaults. They are not evidence of a usable credential and must be replaced with owner-provisioned values in an ignored secret overlay before any runtime or publication activity. No real credential was created, read, logged, or used.
- The current template's framework packages are third-party LGPL-3.0-only ABP components; Microsoft cryptography packages are MIT. LeptonX Lite and generated client/theme assets retain their upstream terms. Only Học-authored additions may carry Apache-2.0; no third-party or generated asset was relabelled.
- A future disposable PostgreSQL verification needs an explicitly approved isolated PostgreSQL target, a non-production database identity, and connection/OpenIddict configuration supplied only through the ignored overlay. Docker remains unavailable and the existing Windows PostgreSQL service remains out of scope. DbMigrator would migrate and seed, so it must not be run until that target and Owner approval are present.

### Status matrix

| Area | Status | Evidence / boundary |
|---|---|---|
| Local SDK selection | PASS | Workspace-local SDK 10.0.403; `global.json` pinned. |
| Solution compilation | PASS | Serialized Release build succeeded; no source compilation errors. |
| Parallel build behavior | UNRESOLVED | One local `MvcTestingAppManifest.json` file-lock failure; use serialized builds pending a reproducible isolated diagnosis. |
| NuGet advisory remediation | PASS | Five advisories mapped; eight locks resolve Xml/Pkcs 10.0.14; targeted CLI audit empty. |
| Build/test warnings | UNRESOLVED | Three generated/external warnings on clean rebuild; none suppressed. |
| Focused Web tests | PASS | 1/1 passed; host-test evidence only. |
| Focused EF tests | PASS | 3/3 passed against in-memory SQLite, not PostgreSQL. |
| PostgreSQL migration and runtime startup | BLOCKED | No approved disposable target; Docker unavailable; existing service untouched. |
| Production/database proof | NOT RUN | No connection, migration, query, or service operation attempted. |
| Publication/license approval | UNRESOLVED | Ignore protections added; upstream notices/terms and template placeholder replacement still require release-time review. |
| H1-02 / DemoRecords | NOT RUN | Explicitly out of scope. |

---

## Continuation evidence — 2026-11-02

**Overall: BLOCKED/INCOMPLETE.** The requested bootstrap progressed; H1-01 is not PASS because no disposable PostgreSQL runtime, migration, local host startup, or database-backed acceptance has been demonstrated.

### PASS

- Microsoft .NET release metadata identified stable SDK **10.0.403**. Its `win-x64` archive SHA-512 was verified before extraction, and it is available only at `.tools/dotnet`; `global.json` pins `10.0.403` with prerelease disabled and no roll-forward. System SDK/PATH were not replaced or persistently modified.
- `Volo.Abp.Cli` **10.7.0** was installed at `.tools/abp-cli` (the executable reports `ABP CLI 10.7.0-dev`). Its own `new --help` confirms `app`, `mvc`, `ef`, `PostgreSQL`, `leptonx-lite`, non-tiered defaults, and `--version` options.
- Exact scaffold command: `abp new Hoc --template app --ui mvc --mobile none --database-provider ef --dbms PostgreSQL --theme leptonx-lite --version 10.7.0 --no-random-port --output-folder <workspace>`. It generated the layered MVC modular-monolith solution `Hoc.slnx`, source projects, tests, PostgreSQL EF provider, DbMigrator, and client assets. No Education/CMS module or custom application feature was added.
- `dotnet restore Hoc.slnx --use-lock-file --force-evaluate` succeeded with local SDK 10.0.403 and produced 15 lock files. ABP packages resolve to 10.7.0; PostgreSQL provider resolves to `Volo.Abp.EntityFrameworkCore.PostgreSql` 10.7.0 / `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3. LeptonX Lite was explicitly changed from the template's stale wildcard to stable free 5.7.0 and restored.
- Generated source has project references between the standard Domain, Application, HttpApi, EntityFrameworkCore, Web, DbMigrator and test projects. A generated PostgreSQL password placeholder and encryption passphrase were removed from `src/Hoc.Web/appsettings.json`; no actual credential was supplied or inspected.

### BLOCKED

- Docker Desktop engine is unavailable. `postgresql-x64-16` is running as an existing automatic Windows service; it was not contacted, started, reconfigured, or used because it may hold an application database. Therefore database migration, DbMigrator, database-backed tests and runtime acceptance are **BLOCKED**.
- The template's automatic migration post-step failed because it invoked the system preview `dotnet`, which rejected pinned SDK 10.0.403. No migration was created or applied. This is safe: direct local-SDK restore succeeds.

### UNRESOLVED

- Restore reports `NU1902` and `NU1903` advisories for transitive `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` 10.0.11. No upgrade was inferred because ABP 10.7 package alignment must remain coherent.
- The ABP CLI maintains per-user cache/telemetry state under `C:\Users\Administrator\.abp` even when the tool executable and NuGet/.NET caches are workspace-local. No global tool, system SDK, service, PATH, or permanent environment change was requested or made; this local tool-state footprint needs a cleanup/containment decision outside H1 source work.
- Framework packages are LGPL-3.0-only and are not Học-owned Apache-2.0 source. LeptonX Lite is free to use, but redistribution notices/terms for all resolved packages and static assets require release-time legal review; no commercial LeptonX, ABP Suite, or paid module was added.

### NOT RUN

- Release build and tests were not run after the corrected restore.
- Local host startup was not run because no approved disposable connection string/database exists; no connection attempt was made.
- No Docker service/container, PostgreSQL migration, schema change, DemoRecords, H1-02, remote, repository publication, or production system action was performed.

---

## H1-01C Isolated PostgreSQL Runtime, Migration Proof, and Safe Shutdown — 2026-11-02

**Scope:** `D:\job\OaiPhongLabs\HocTat\Hoc` only. This evidence uses a new disposable PostgreSQL 16.14 cluster under the ignored workspace `.tools` directory. No existing Windows PostgreSQL service, existing database, Docker setting, firewall, system PATH, remote, GitHub repository, commit, push, deployment, or H1-02 work was touched.

### Process identity and isolation

The two listeners were independently matched to workspace-owned command lines before validation and shutdown:

| Listener | Observed owner | Identity result |
|---|---|---|
| `127.0.0.1:55432` | `C:\Program Files\PostgreSQL\16\bin\postgres.exe -D D:\job\OaiPhongLabs\HocTat\Hoc\.tools\pg\h1-01c\data` | PASS — PostgreSQL 16 disposable cluster, exact isolated data directory. |
| `127.0.0.1:51234` | `src\Hoc.Web\bin\Debug\net10.0\Hoc.Web.exe --urls http://127.0.0.1:51234` | PASS — Hoc Web host launched from this workspace. |

`postgresql.auto.conf` fixes the disposable cluster to `port = 55432` and `listen_addresses = '127.0.0.1'`. `pg_ctl status -D .tools\pg\h1-01c\data` independently reported the same executable, PID, and data directory. Port `5432` and all unrelated `dotnet.exe`, MSBuild node, and PostgreSQL processes were outside the shutdown target.

### PostgreSQL authentication disposition

`pg_hba.conf` contains only `trust` rules for local and loopback traffic (`127.0.0.1/32` and `::1/128`); the isolated role/database are both named `hoc_h101c`. This was a local execution fallback after the environment blocked secure creation of a generated password file before it executed. It is acceptable only for this short-lived, loopback-only disposable proof.

**UNRESOLVED — password authentication:** SCRAM/password configuration was not tested. `trust` authentication is not an acceptable reusable development, CI, shared-host, or production security baseline. No setting outside the disposable cluster was weakened, and no credential was printed, stored in source, logged, or committed.

### Migration ownership, execution, and schema proof

The scaffold initially had no `Migrations` directory. The generated `HocDbContextFactory` is in `Hoc.EntityFrameworkCore`; the DbMigrator project is not a valid EF tooling startup project because it does not reference `Microsoft.EntityFrameworkCore.Design`. The documented safe tooling path was therefore:

```powershell
.\.tools\dotnet\dotnet.exe build .\src\Hoc.DbMigrator\Hoc.DbMigrator.csproj --no-restore --configuration Debug -m:1 /nr:false
.\.tools\dotnet\dotnet.exe ef migrations add InitialCreate `
  --project .\src\Hoc.EntityFrameworkCore\Hoc.EntityFrameworkCore.csproj `
  --startup-project .\src\Hoc.EntityFrameworkCore\Hoc.EntityFrameworkCore.csproj `
  --context Hoc.EntityFrameworkCore.HocDbContext --output-dir Migrations --no-build
```

This created `20261102145512_InitialCreate`, its designer, and `HocDbContextModelSnapshot`. The source migration targets Npgsql/PostgreSQL (`uuid`, `character varying`, Npgsql identity annotations) and creates the ABP module tables. The `Up` path is additive initial schema creation; its `Down` contains table drops as normal for an initial migration and was not run. No existing migration was rewritten and no migration was changed to bypass a runtime/configuration failure.

`HocDbMigrationService` owns the execution sequence: migrate the host schema through `EntityFrameworkCoreHocDbSchemaMigrator`/`HocDbContext.Database.MigrateAsync`, seed the host, then enumerate and migrate tenant-specific connection strings if present. The first local DbMigrator run completed migration and seed. The first EF history lookup logged an expected missing-history-table command before the initial migration created it; the migrator then completed successfully.

The second DbMigrator run was the repeatability check. Before and after it, direct loopback `psql` evidence was identical:

| Check | Before second run | After second run | Result |
|---|---:|---:|---|
| `__EFMigrationsHistory` rows | 1 | 1 | PASS — no additional migration applied. |
| `public` base tables | 39 | 39 | PASS — schema stable. |
| seeded `admin` user rows | 1 | 1 | PASS — seed repeatable for this baseline. |
| `OpenIddictApplications` rows | 0 | 0 | FACT — no client application seed was observed; not a login proof. |

The applied history entry is `20261102145512_InitialCreate|10.0.11`.

### Host runtime and HTTP evidence

The host started in Development mode with `Now listening on: http://127.0.0.1:51234` and `Application started` in the application log. Its connection string and local URL override were stored through the existing Web `UserSecretsId`, outside the checkout; values are intentionally not reproduced here. The committed ignored-overlay convention was also verified with `git check-ignore` for both `appsettings.secrets.json` paths.

All requests below were made to the isolated loopback host. Redirect following was disabled for the protected-route check.

| Request | Status | Result |
|---|---:|---|
| `/` | 200 `text/html` | PASS — host responds. |
| `/Account/Login` | 200 `text/html` | PASS — generated login page responds. |
| `/Themes/LeptonXLite/Global/side-menu/css/layout-bundle.css` | 200 `text/css` | PASS — LeptonX Lite layout asset served. |
| `/Themes/LeptonXLite/Global/side-menu/css/js-bundle.css` | 200 `text/css` | PASS — LeptonX Lite JavaScript-bundle stylesheet served. |
| `/libs/abp/core/abp.css` | 200 `text/css` | PASS — ABP static asset served. |
| `/Identity/Users` anonymous | 302 | PASS — redirects to `/Account/Login?ReturnUrl=%2FIdentity%2FUsers`; authorization denial is enforced. |

**UNVERIFIED — visual UI and Admin shell:** the computer-use controller stopped because it could not determine the current Chrome URL with enough confidence to enforce its policy. No browser screenshot or rendered navigation assertion was recorded. This is an automation-controller/access limitation, not evidence of a visual failure.

**NOT RUN — login/logout:** no valid credential was supplied through a secure channel. No credential was guessed, copied from a template, logged, or sent to the host.

### Dependency and Git-ignore review

The direct `System.Security.Cryptography.Xml` `10.0.14` overrides in Application, Domain, EntityFrameworkCore, and DbMigrator retain `PrivateAssets="all"`. The EntityFrameworkCore and DbMigrator resolved assets include both `System.Security.Cryptography.Xml/10.0.14` and its exact `System.Security.Cryptography.Pkcs/10.0.14` dependency. Eight affected lock files resolve Xml `10.0.14` as a direct asset.

**UNRESOLVED — Web runtime closure:** `Hoc.Web` does not directly declare either override and its `project.assets.json`/Debug output did not contain a package-resolution entry or copied DLL for Xml/Pkcs. `PrivateAssets="all"` intentionally prevents those references from flowing to consuming projects. The successful Web startup is runtime connectivity proof, but it is not a complete attestation of the Web host's cryptography package closure; no dependency change was made in this bounded task.

`git check-ignore -v` confirmed `.tools/` (cluster data/logs), `**/appsettings.secrets.json`, and `*.pfx` are ignored. Hypothetical alternate roots such as `config/local`, `.local`, and `runtime` are not covered by the current ignore rules and were not used. The Git repository remains an unborn `main` branch with no `HEAD` and no tracked files; nothing was staged or committed.

### Build and tooling limitations

- PASS: serialized Debug build of DbMigrator completed with 0 warnings/0 errors before migration application.
- UNRESOLVED: an initial normal EF tooling build hit `MSB3713` because `Hoc.Domain.Shared.AssemblyInfo.cs` was locked. A serialized `-m:1 /nr:false` build succeeded. H1-01B had separately observed an `MvcTestingAppManifest.json` output lock under a parallel solution build. No source workaround was introduced; serial builds remain the safe local path pending diagnosis.
- UNRESOLVED: `dotnet-ef` is 10.0.8 while the EF runtime is 10.0.11. Migration generation completed successfully but emitted the version-skew warning. No global tooling was changed.

### H1-01C status matrix

| Area | Status | Boundary |
|---|---|---|
| Isolated PostgreSQL process/data/port | PASS | Workspace data directory, port 55432, and loopback listener independently matched. |
| Existing Windows PostgreSQL service safety | PASS | Never connected to, started, stopped, or reconfigured. |
| Initial Npgsql migration creation/application | PASS | InitialCreate generated, applied, and recorded in EF history. |
| ABP schema/seed and repeatable migration run | PASS | 39 tables, one history row, one admin row remained stable on second run. |
| Host database connectivity/startup | PASS | Workspace Web host listened on 51234 and served HTTP responses from the migrated database configuration. |
| LeptonX Lite static asset delivery | PASS | Three referenced CSS assets returned 200. |
| Anonymous protected-route behavior | PASS | `/Identity/Users` redirected to Login without redirect-following. |
| Browser visual rendering/Admin navigation | UNVERIFIED | Browser controller policy blocked safe visual inspection. |
| Login/logout | NOT RUN | No secure credential handoff; none invented. |
| PostgreSQL password/SCRAM authentication | UNRESOLVED | Disposable cluster uses loopback-only trust; not reusable security proof. |
| Cryptography resolved closure | UNRESOLVED | EF/DbMigrator override closure verified; Web host closure not directly attested because private assets do not transitively flow. |
| Git ignore protection | PASS | `.tools`, secrets overlays, and certificates verified ignored. |
| Parallel/clean-build reliability | UNRESOLVED | Prior parallel output locks; serialized build succeeds. |
| H1-02, GitHub, commit/push/deployment/production | NOT RUN | Explicitly outside scope. |

### Shutdown procedure and result

The verified Hoc Web console received a graceful Ctrl+C. Its log records `Application is shutting down` and the OpenIddict token-cleanup worker stopping; the workspace host process tree and port 51234 then disappeared.

After revalidating the PostgreSQL listener executable, loopback address, command line, and exact `.tools\pg\h1-01c\data` directory, the matching PostgreSQL 16 controller stopped only that cluster:

```powershell
C:\Program Files\PostgreSQL\16\bin\pg_ctl.exe stop `
  -D .tools\pg\h1-01c\data -m fast -w -t 30
```

`pg_ctl` reported `server stopped`; a subsequent status check reported `no server running` (exit 3), and both `127.0.0.1:51234` and `127.0.0.1:55432` were closed. `PG_VERSION`, `postgres.log`, the ignored data directory, migration source, and this report were preserved. No shared service/process/data was terminated or deleted.

---

## H1-P01 Pre-Public Repository Audit — 2026-11-03

**Overall disposition: BLOCKED — Owner NO-GO for creating `letienhoang/Hoc`.** This is a read-only publication audit, except for the ignored local Web publish artifact used to inspect its dependency graph. No repository was created, no commit/staging/push/package publication/deployment occurred, and no database, service, remote, or production system was accessed.

### Evidence boundary and Git state

| Check | Status | Evidence |
|---|---|---|
| Git root | PASS | `git rev-parse --show-toplevel` returned `D:/job/OaiPhongLabs/HocTat/Hoc`. |
| Repository history / staging / remote | PASS | Unborn `main`: no `HEAD`, zero commits, zero remotes, and zero staged paths. Therefore no prior Git history exists to scan and no candidate file is implicitly approved for first publication. |
| Candidate set | UNRESOLVED | `git ls-files --others --exclude-standard` returned 172 unignored, untracked paths. All candidate content requires an explicit first-commit allowlist. |
| Existing ignored local state | PASS | Actual workspace `.tools/pg/h1-01c/data`, source Web logs, `node_modules`, `wwwroot/libs` contents, and all three observed `appsettings.secrets.json` overlays are ignored. |
| Ignore policy for common secret/log overlays | BLOCKED | `.env`, `.env.local`, `secrets.env`, `certs/dev.key`, `certs/dev.pem`, `appsettings.Production.json`, `appsettings.Local.json`, `docker-compose.override.yml`, and root `Logs/logs.txt` are **not** ignored. The root log exists and is unignored. |

The existing rules correctly exclude `bin/`, `obj/`, `.vs/`, `node_modules/`, `.tools/`, `*.pfx`, `**/appsettings.secrets.json`, and `**/wwwroot/libs/*`. They do not provide a safe general policy for environment overlays, private-key formats, production/local configuration, root logs, or compose overrides. Do not use `git add .` in this unborn repository.

### Secrets, credentials, certificates, and generated configuration

| Area | Status | Evidence and required disposition |
|---|---|---|
| Private-key content scan | PASS (bounded) | No PEM/private-key marker was found in unignored candidate source/configuration outside the excluded build/tool/cache trees. This does not validate future additions or ignored files. |
| Secret overlays and local data | PASS | Existing secret overlays and isolated PostgreSQL data are ignored as above; no overlay value is reproduced in this report. |
| Template connection string | UNRESOLVED | `src/Hoc.DbMigrator/appsettings.json` has a local/template `Default` connection string containing a password component. It is not an observed production credential, but public publication needs an explicit non-secret example or a documented external configuration mechanism. |
| Literal template authentication values | BLOCKED | `src/Hoc.DbMigrator/appsettings.json` contains a nonempty `ClientSecret` value and `test/Hoc.HttpApi.Client.ConsoleTestApp/appsettings.json` a nonempty `UserPassword` value. Their provenance was not established as deliberately public, inert fixtures. Replace with explicit public-safe fakes or move to ignored test/secret configuration before publication. |
| README certificate material | BLOCKED | `README.md` publishes a literal password in a `dotnet dev-certs ... -ep openiddict.pfx -p ...` command and says it is the certificate password. Treat it as a hard-coded credential pattern even if template-derived: remove it, do not create/reuse the referenced PFX, and document owner-controlled certificate provisioning without an inline password. |
| Certificates / keys on disk | PASS (bounded) | No unignored certificate/key filename was found. `*.pfx` is ignored, but PEM/key variants are not, so this is not a future-proof protection. |
| Logs | BLOCKED | `Logs/logs.txt` is unignored. Its bounded scan found no credential-pattern match, but it contains an error line and is runtime output, not public source. Exclude it. The ignored Web log also contains credential-pattern matches and must remain excluded. |

Static scans used redacted classifications and filenames/line categories only; no configuration, log, token, password, connection-string, certificate, or user-secret value is carried into this report.

### Dependency, package, and license inventory

All 15 candidate `packages.lock.json` files target `net10.0`. These locks are the reproducible dependency inventory proposed for first commit: the 15 project graphs contain 2--281 package nodes each, and deduplicate to **318 distinct ID/version pairs**: 142 `Volo.Abp.*`, 79 `Microsoft.*`, and 14 `System.*` pairs.

| Lock graph | Direct | Transitive | Total |
|---|---:|---:|---:|
| Application.Contracts | 7 | 78 | 86 |
| Application | 7 | 130 | 140 |
| DbMigrator | 7 | 155 | 166 |
| Domain.Shared | 9 | 61 | 70 |
| Domain | 11 | 108 | 120 |
| EntityFrameworkCore | 11 | 145 | 158 |
| HttpApi.Client | 6 | 97 | 105 |
| HttpApi | 6 | 120 | 128 |
| Web | 10 | 185 | 201 |
| Application.Tests | 2 | 161 | 169 |
| Domain.Tests | 2 | 143 | 148 |
| EntityFrameworkCore.Tests | 3 | 186 | 197 |
| HttpApi.Client.ConsoleTestApp | 4 | 120 | 127 |
| TestBase | 11 | 73 | 84 |
| Web.Tests | 4 | 266 | 281 |

The Web graph has 10 direct and 185 transitive packages (201 total). The following are the direct Web package roots:

```text
Serilog.AspNetCore 9.0.0
Serilog.Sinks.Async 2.1.0
Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite 5.7.0
Volo.Abp.Autofac 10.7.0
Volo.Abp.Swashbuckle 10.7.0
Volo.Abp.AspNetCore.Serilog 10.7.0
Volo.Abp.Identity.Web 10.7.0
Volo.Abp.Account.Web.OpenIddict 10.7.0
Volo.Abp.TenantManagement.Web 10.7.0
Volo.Abp.SettingManagement.Web 10.7.0
```

| License evidence | Status | Result |
|---|---|---|
| NuGet metadata coverage | PASS | Of 318 lock identities, 306 are third-party packages with locally resolved `.nuspec` metadata; the other 12 are Học project references, not external packages. |
| NuGet expressions | UNRESOLVED | Observed metadata groups: LGPL-3.0-only (141), MIT (115), Apache-2.0 (36), BSD-2-Clause (1), BSD-3-Clause (6), PostgreSQL (2), and three file-reference license fields. Three packages have no SPDX expression: `NUglify` 1.21.17, `xunit.abstractions` 2.0.3, and LeptonX Lite 5.7.0. Two further packages point at `LICENSE.md`/`LICENSE.txt` rather than an SPDX expression. Collect, review, and preserve their upstream license texts before publication. |
| ABP framework/modules | UNRESOLVED | Representative ABP NuGet metadata (`Volo.Abp.Core` 10.7.0) declares LGPL-3.0-only. The 141 resolved ABP identities must remain third-party components; Apache-2.0 cannot be applied to their source/binaries. Owner/legal review must establish the required LGPL notices, source-offer/corresponding-source approach where applicable, and the intended redistribution model. |
| LeptonX Lite | UNRESOLVED | The NuGet package 5.7.0 has no license field or bundled license file and points to a commercial ABP project URL. Its installed npm package declares `LGPL-3.0`. This mismatch/incompleteness requires authoritative upstream license/redistribution clarification before publishing source or copied assets. |
| Client libraries | UNRESOLVED | `yarn.lock` pins the LeptonX Lite npm graph and copied ignored `wwwroot/libs` assets. Representative installed package metadata is LGPL-3.0 for ABP/LeptonX, `(CC-BY-4.0 AND OFL-1.1 AND MIT)` for Font Awesome, and MIT for Bootstrap, jQuery, SweetAlert2, Select2, and Moment. Their required notices/license texts are not yet assembled. |
| Generated/template assets | UNRESOLVED | Fourteen unignored raster/SVG assets under `src/Hoc.Web/wwwroot/images` (including LeptonX logos and getting-started/social assets) have no local provenance/attribution manifest. The generated migration, EF model snapshot, `abp.resourcemapping.js`, template pages, and package locks require upstream/template provenance to be recorded; do not relabel them wholesale as Học Apache-2.0 work. |
| Học license documents | BLOCKED | No root `LICENSE*` or `NOTICE*` exists. Apache-2.0 must be added for Học-owned source only, with a scoped copyright/ownership statement. A NOTICE/third-party attribution process remains required; it must not assert that ABP, LeptonX, generated assets, or dependencies are Apache-2.0. |

### Cryptography runtime/publish closure

An offline local publish was produced under ignored `.tools/audit/h1-p01-web-publish`; it was not started or deployed. It contains 199 dependency-target nodes. `src/Hoc.Web/packages.lock.json` has no direct or transitive `System.Security.Cryptography.Xml`, `System.Security.Cryptography.Pkcs`, `System.Security.Cryptography.Cng`, or `System.Security.Cryptography.ProtectedData` package identity.

The application/library locks that carry the pre-existing private Xml remediation resolve `System.Security.Cryptography.Xml` and `System.Security.Cryptography.Pkcs` at 10.0.14 in their own graphs. Those private assets do not create a corresponding Web lock node. The publish runtime config remains framework-dependent and specifies compatible framework version `10.0.0`; its `refs` directory contains Xml/Cng reference assemblies from local 10.0.14 servicing, while Pkcs is not present as a publish-package node or copied runtime assembly.

| Check | Status | Consequence |
|---|---|---|
| Historical library-graph remediation | PASS | The existing Xml/Pkcs 10.0.14 lock resolutions remain present in the affected non-Web graphs. |
| Web package graph attestation | PASS | Current Web lock and local publish manifest were inspected; neither carries Xml/Pkcs as a NuGet dependency. |
| Web deployment cryptography closure | UNRESOLVED | A framework-dependent release can bind to a servicing runtime selected by its deployment host. The local SDK/reference pack being 10.0.14 is not evidence that every future host has fixed servicing bits. Establish a supported runtime baseline/roll-forward policy and produce an SBOM or deployment-target dependency attestation before GO. |
| Current all-package vulnerability scan | UNRESOLVED | `dotnet package list --vulnerable --include-transitive --no-restore` was invoked with the workspace SDK but did not complete in the bounded audit window; no clean advisory result is claimed. The spawned local audit processes had exited by final observation. Re-run against approved advisory sources and archive the result. |

### Publication classification

| Classification | Paths/content | Status |
|---|---|---|
| Potentially public after blockers close | `Hoc.slnx`, `global.json`, `common.props`, project files, lock files, Học-authored C# source/tests, and ordinary static content | UNRESOLVED — use an explicit allowlist, retain lock files, add the scoped Apache-2.0 license, and complete third-party/generated provenance review. |
| Public only after redaction/replacement | `README.md`; `src/Hoc.DbMigrator/appsettings.json`; `test/Hoc.HttpApi.Client.ConsoleTestApp/appsettings.json`; `src/Hoc.Web/appsettings.Development.json` | BLOCKED — remove literal certificate/password guidance and convert all template authentication/connection settings to provably non-secret public examples or ignored external configuration. |
| Exclude from the first public commit | `.tools/`, `bin/`, `obj/`, `.vs/`, `node_modules/`, `wwwroot/libs/`, all `appsettings.secrets.json`, all PFX/cert/key/private-key material, PostgreSQL data/logs, test results, user settings, and any publish output | PASS for the existing protected paths; BLOCKED for formats/overlays not covered by `.gitignore`. |
| Exclude or redact before publication | `Logs/logs.txt`; private technical evidence containing local paths/runtime details; template/logo/getting-started assets without an approved provenance decision | BLOCKED/UNRESOLVED. |

### Required Owner gates before repository creation

1. **NO-GO until critical secrets/configuration blockers close:** remove the README password, classify/remove the two literal template authentication values, and make the connection-string example explicitly non-secret.
2. **NO-GO until the ignore policy is extended and rechecked:** cover environment/secret overlays, PEM/key formats, production/local settings, compose overrides, and root logs; then validate a named first-commit allowlist rather than broad staging.
3. **NO-GO until licensing is complete:** add scoped Apache-2.0 `LICENSE` material for Học-owned code, a third-party NOTICE/attribution approach, exact ABP LGPL obligations, authoritative LeptonX Lite terms, and provenance decisions for generated/template source and 14 public image/SVG assets.
4. **NO-GO until supply-chain evidence is current:** complete and retain an approved-source vulnerability audit for all 15 lock graphs, resolve/document packages without SPDX metadata, and pin/attest the Web deployment runtime's cryptography servicing baseline.
5. Only after all above are independently marked PASS may the Owner authorize repository creation and an explicit named-path initial commit. That authorization is not granted by this report.

---

## H1-P02 Public Source Hardening — 2026-11-03

**Overall disposition: NO-GO.** This bounded local hardening pass remediated the verified public-source secret/default and ignore-policy issues, added an Apache-2.0 `LICENSE` scoped to Học-owned material, and recorded a third-party attribution inventory. It did **not** create a repository, stage/commit/push, contact GitHub, deploy, start an application, or access a database, service, or production system. Owner authorization and Technical Lead/legal review remain required before publication.

### Exact files changed

| Path | Change |
| --- | --- |
| `.gitignore` | Added ignores for environment/local/production overlays, Compose overrides, certificates/private keys/signing keys, local database files, and root/nested `Logs/`; retained `.tools/`, secret overlay, build, restored-library, and safe-source coverage. |
| `README.md` | Replaced the literal certificate password command with the `OpenIddict__CertificatePassword` environment-variable convention and secret-handling instructions. |
| `src/Hoc.DbMigrator/appsettings.json` | Removed a password-bearing connection-string value and the confidential-client secret. |
| `test/Hoc.HttpApi.Client.ConsoleTestApp/appsettings.json` | Removed test username/password defaults; the existing ignored `appsettings.secrets.json` mechanism remains available. |
| `src/Hoc.Domain/OpenIddict/OpenIddictDataSeedContributor.cs` | Removed the confidential-client secret fallback; absent confidential-client configuration now fails through the existing validation path. |
| `src/Hoc.Web/HocWebModule.cs` | Replaced the production certificate-password literal with required `OpenIddict:CertificatePassword` configuration; a non-development host fails closed when it is absent. |
| `LICENSE` | Added the unmodified official Apache License, Version 2.0 text. |
| `NOTICE` | Added a scope-safe notice that does not assert ownership of third-party material. |
| `THIRD-PARTY-NOTICES.md` | Added dependency, generated-source, static-asset, provenance, license, and distribution inventory. |
| `reports/H1-01-technical-preflight.md` | Added this H1-P02 evidence. |

### Secrets and repository safety

The Git root remains `D:/job/OaiPhongLabs/HocTat/Hoc`, on unborn `main`, with no commits, no remote, and no staged paths. The prior H1-P01 candidate set had 172 unignored/untracked paths. Every member was included in the redacted candidate scan/review. After the changes, `git ls-files --others --exclude-standard` and `git status --short --untracked-files=all` both report 174 candidates: root `Logs/logs.txt` is now excluded and the three new publication documents are candidates. This does not approve a first commit; an explicit named-path allowlist is still mandatory.

The public-source scan found and remediated four hard-coded credential/default locations: the DbMigrator connection-string password, DbMigrator OpenIddict client secret, console test password, README certificate-password example, plus the subsequently discovered OpenIddict seed fallback and Web production-certificate password literal. The final known-default scan found no remaining occurrences. The remaining semantic `ClientSecret`/`Password` identifiers occur only in configuration lookup, validation, schema/migration metadata, or property names; they are not literal credential values.

`git check-ignore -v --no-index` produced PASS evidence for representative `.tools/`, `.env`, `.env.local`, nested `.env`, `appsettings.secrets.json`, local/production appsettings overlays, `.key`, `.pem`, `.pfx`, `.p12`, Compose overrides, root `Logs/logs.txt`, nested application logs, and restored `wwwroot/libs` paths. It also confirmed that `README.md` and the safe public `appsettings.Development.json` remain unignored. JSON parsing passed for all four inspected public appsettings files.

The DbMigrator, Web, and console programs already invoke `AddAppSettingsSecretsJson`; thus ignored local `appsettings.secrets.json` overlays remain the supported development configuration route. In non-development Web hosting, `OpenIddict__CertificatePassword` can supply `OpenIddict:CertificatePassword` through standard configuration binding. No secret value is recorded here.

### Licensing, attribution, and provenance

`LICENSE` is the official Apache License 2.0 text from [Apache](https://www.apache.org/licenses/LICENSE-2.0.txt). Its scope is explicitly limited to Học-authored material after copyright ownership is attested; it does not relicense dependencies, template-derived source, generated material, or static assets. `NOTICE` and `THIRD-PARTY-NOTICES.md` retain that boundary.

| Component/material | Version or count | Evidence | Status |
| --- | ---: | --- | --- |
| ABP Framework/OSS modules | 141 resolved `Volo.Abp.*` identities, principally 10.7.0 | Representative `Volo.Abp.Core` NuGet metadata declares `LGPL-3.0-only`; upstream repository: https://github.com/abpframework/abp | UNRESOLVED — legal review must establish exact notices and any corresponding-source/distribution obligations. |
| LeptonX Lite NuGet theme | 5.7.0 | Local NuGet metadata has no license expression or bundled license; source: https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite/5.7.0 | UNRESOLVED — obtain authoritative terms and copied-asset redistribution decision. |
| LeptonX Lite npm package | `~5.7.0`, locked in `yarn.lock` | Installed package metadata declares LGPL-3.0; source: https://www.npmjs.com/package/@abp/aspnetcore.mvc.ui.theme.leptonxlite | UNRESOLVED — reconcile with NuGet metadata and preserve required notices. |
| NuGet graph | 15 locks; 318 distinct external ID/version pairs | Lock files are the exact per-project inventory; 79 `Microsoft.*`, 14 `System.*` pairs, plus other components | UNRESOLVED — retain required package-specific notices. |
| Client assets | versions locked in `yarn.lock` | Representative metadata: Bootstrap, jQuery, SweetAlert2, Select2, Moment MIT; Font Awesome `(CC-BY-4.0 AND OFL-1.1 AND MIT)` | UNRESOLVED — preserve exact upstream notices for any copied/distributed assets. |
| Generated source | 3 candidate paths: EF migration designer, model snapshot, `abp.resourcemapping.js` | ABP/template-derived | UNRESOLVED — retain provenance; do not label wholesale as Học-owned. |
| Static image/SVG assets | 14 under `src/Hoc.Web/wwwroot/images/` | LeptonX/template-looking assets; no local provenance manifest | UNRESOLVED — obtain upstream asset provenance/redistribution approval. |

The follow-up inventory retains packages with non-SPDX or file-only NuGet license metadata: `NUglify` 1.21.17, `xunit.abstractions` 2.0.3, and LeptonX Lite 5.7.0. Their upstream texts must be retrieved and preserved before distribution. This is an attribution record, not legal clearance.

### Dependency security and Web runtime cryptography

The complete command below completed against the installed workspace SDK and its configured NuGet source (`https://api.nuget.org/v3/index.json`), producing all 15 project entries and no vulnerable-package entries:

```powershell
.\.tools\dotnet\dotnet.exe list .\Hoc.slnx package --vulnerable --include-transitive --format json --output-version 1 --no-restore
```

**PASS — current lock-graph NuGet vulnerability audit completed cleanly.** It is a point-in-time advisory result, not a guarantee for future restores or deployment environments.

The inspected lock files resolve `System.Security.Cryptography.Xml` 10.0.14 directly and `System.Security.Cryptography.Pkcs` 10.0.14 transitively in Application, DbMigrator, Domain, EntityFrameworkCore, and their affected test graphs (eight locks total). The current Web lock has neither package. The existing ignored Web publish artifact was inspected without execution: its `.deps.json` has zero Xml/Pkcs package nodes and the publish root has zero `System.Security.Cryptography.*` runtime DLLs. Its reference directory includes `System.Security.Cryptography.Xml.dll`, but no Pkcs reference assembly. The runtime configuration is framework-dependent (`Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` 10.0.0). Therefore:

| Finding | Status | Boundary |
| --- | --- | --- |
| Library/test lock remediation at Xml/Pkcs 10.0.14 | PASS | Present in the eight affected lock files. |
| Current Web package/publish manifest has no Xml/Pkcs package node or copied runtime DLL | PASS | Direct local artifact inspection only. |
| Future Web deployment cryptography servicing closure | UNRESOLVED | A framework-dependent host selects its installed runtime; establish supported servicing/roll-forward policy and deployment-target attestation before GO. |

No NuGet warning suppression or dependency upgrade was introduced.

### Bounded validation

| Check | Result |
| --- | --- |
| Candidate secret scan | PASS for literal/private-key/credential-pattern scan after remediation; remaining name-only matches were manually classified as non-secret semantics or schema metadata. |
| Ignore behavior | PASS through `git check-ignore`, as detailed above. |
| Public configuration parse | PASS — inspected JSON files parse. |
| Release build | PASS — final `dotnet build Hoc.slnx --no-restore --configuration Release -m:1 /nr:false` completed with 0 errors. The observed warnings were pre-existing/template-generated `CS0162` in `HocMenuContributor.cs` and `CS7022` in Microsoft.NET.Test.Sdk generated source. |
| Tests | NOT RUN — this change is source/configuration/license hardening; no isolated test/database target was authorized or needed for the compile-time change. |
| `git diff --check` | PASS. The checkout is unborn, so this validates no tracked diff; untracked first-commit review still requires an explicit allowlist. |
| External/production activity | PASS — no external service, database, production system, repository, staging, commit, push, or deployment action was performed. |

### Publication-readiness matrix and recommendation

| Gate | Status | Owner/action required |
| --- | --- | --- |
| Hard-coded secrets/default credentials | PASS | Removed or converted to ignored/external configuration. |
| Ignore coverage | PASS | Retain `git check-ignore` verification in the eventual named-path commit review. |
| Candidate inventory | PASS (reviewed), not approved | Technical Lead must approve an explicit first-commit allowlist. |
| Học-owned Apache-2.0 scope | UNRESOLVED | Owner/copyright holder must attest ownership and intended scope. |
| ABP/LeptonX/static-asset licensing | BLOCKED | Legal/Owner decision on obligations, authoritative LeptonX terms, notices, and 14 asset provenance records. |
| Dependency advisory audit | PASS | Re-run against approved sources at the final publication commit. |
| Web runtime cryptography closure | UNRESOLVED | Deployment owner must attest target runtime servicing/roll-forward baseline. |
| Repository creation/commit/push/deployment | NOT AUTHORIZED | Owner NO-GO remains in force. |

**Recommendation: NO-GO.** Do not create `letienhoang/Hoc`, commit, push, tag, package, or deploy. The secret/ignore remediation and current dependency audit are complete, but licensing/provenance, ownership, target-runtime attestation, Technical Lead allowlist review, and separate Owner authorization remain critical publication gates.

## H1-P03 License and Distribution Closure — 2026-11-03

**Technical Lead recommendation: NO-GO.** The owner NO-GO remains binding. Independently, the exact first-commit candidate contains 14 copied static assets with no authoritative provenance or redistribution terms, and the evidence does not establish the template-output provenance/attribution package needed to call the source distribution closed. This is an evidence report, not legal advice or a legal-clearance determination. No repository, staging area, commit, push, package, deployment, production service, database, or real certificate was used.

### Scope, baseline, and evidence vocabulary

- **FACT:** the Git repository is on unborn `main`; `HEAD` does not resolve; there are zero commits, zero remotes, and zero staged paths. `git ls-files --others --exclude-standard` returned the 174 paths enumerated below. This is the exact candidate set at review time, not an approved allowlist.
- **FACT:** local NuGet metadata contains 141 `Volo.Abp.*` 10.7.0 nuspecs, each declaring `LGPL-3.0-only` as an expression. Representative local package: `Volo.Abp.Core` 10.7.0, repository commit `9a85bacf937c839b98ee3a2caa849c119d8dd49f`.
- **FACT:** `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` 5.7.0 has no NuGet `license` element and no bundled license file in its restored package. Its locked npm counterpart `@abp/aspnetcore.mvc.ui.theme.leptonxlite` 5.7.0 declares `LGPL-3.0` and names `https://github.com/volosoft/lepton.git`; those facts do not resolve the missing NuGet redistribution terms.
- **INFERENCE:** the layered solution, starter pages, resource mapping, project layout, and README are generated from an ABP starter template. No generator command, template archive, or per-file provenance manifest is present in this workspace, so this is not sufficient to attribute each output file to a specific upstream source revision.
- **UNRESOLVED:** Học copyright ownership/authority has not been attested. The root Apache-2.0 text therefore cannot be treated as a verified grant for any file beyond the scope stated in `NOTICE`.

### Exact publication candidate manifest

The manifest is exhaustive and disjoint: 174 paths. A category identifies provenance/distribution treatment, not a conclusion that a legal right has been granted. `Referenced only` means the source commit carries a declaration or integrity record, not the package binary or restored client asset.
#### Học-authored publication documents (4)
Origin: Created for this local hardening/preflight work; copyright authority is still unattested.
License evidence: Root Apache-2.0 text and scoped NOTICE; no independent ownership attestation.
Publication disposition: BLOCKED pending Owner copyright attestation and final named-path allowlist.
```text
LICENSE
NOTICE
THIRD-PARTY-NOTICES.md
reports/H1-01-technical-preflight.md
```

#### Học-authored publication controls (2)
Origin: Local H1-P02 hardening changes.
License evidence: Intended to be within scoped root Apache-2.0; authority remains unattested.
Publication disposition: BLOCKED pending the same ownership gate; controls are not third-party redistribution.
```text
.gitattributes
.gitignore
```

#### ABP-generated application/template output (including Học modifications where present) (120)
Origin: ABP layered application-template provenance is inferred from project structure and embedded starter documentation; exact generating template/revision is not recorded in the workspace.
License evidence: ABP Framework 10.7.0 local NuGet metadata: LGPL-3.0-only; upstream repository license: https://github.com/abpframework/abp/blob/10.7.0/LICENSE.md . This is a family-level evidence record, not a per-file source match.
Publication disposition: BLOCKED. Preserve ABP attribution and determine the applicable LGPL source/notice method before any source distribution. Do not relabel these files as solely Học-owned.
```text
.prettierrc
Hoc.slnx
README.md
common.props
global.json
src/Hoc.Application.Contracts/HocApplicationContractsModule.cs
src/Hoc.Application.Contracts/HocDtoExtensions.cs
src/Hoc.Application.Contracts/Permissions/HocPermissionDefinitionProvider.cs
src/Hoc.Application.Contracts/Permissions/HocPermissions.cs
src/Hoc.Application/HocAppService.cs
src/Hoc.Application/HocApplicationMappers.cs
src/Hoc.Application/HocApplicationModule.cs
src/Hoc.Application/Properties/AssemblyInfo.cs
src/Hoc.DbMigrator/DbMigratorHostedService.cs
src/Hoc.DbMigrator/HocDbMigratorModule.cs
src/Hoc.DbMigrator/Program.cs
src/Hoc.DbMigrator/appsettings.json
src/Hoc.Domain.Shared/HocDomainErrorCodes.cs
src/Hoc.Domain.Shared/HocDomainSharedModule.cs
src/Hoc.Domain.Shared/HocGlobalFeatureConfigurator.cs
src/Hoc.Domain.Shared/HocModuleExtensionConfigurator.cs
src/Hoc.Domain.Shared/Localization/Hoc/ar.json
src/Hoc.Domain.Shared/Localization/Hoc/cs.json
src/Hoc.Domain.Shared/Localization/Hoc/de.json
src/Hoc.Domain.Shared/Localization/Hoc/en-GB.json
src/Hoc.Domain.Shared/Localization/Hoc/en.json
src/Hoc.Domain.Shared/Localization/Hoc/es.json
src/Hoc.Domain.Shared/Localization/Hoc/fi.json
src/Hoc.Domain.Shared/Localization/Hoc/fr.json
src/Hoc.Domain.Shared/Localization/Hoc/hi.json
src/Hoc.Domain.Shared/Localization/Hoc/hr.json
src/Hoc.Domain.Shared/Localization/Hoc/hu.json
src/Hoc.Domain.Shared/Localization/Hoc/is.json
src/Hoc.Domain.Shared/Localization/Hoc/it.json
src/Hoc.Domain.Shared/Localization/Hoc/nl.json
src/Hoc.Domain.Shared/Localization/Hoc/pl-PL.json
src/Hoc.Domain.Shared/Localization/Hoc/pt-BR.json
src/Hoc.Domain.Shared/Localization/Hoc/ro-RO.json
src/Hoc.Domain.Shared/Localization/Hoc/ru.json
src/Hoc.Domain.Shared/Localization/Hoc/sk.json
src/Hoc.Domain.Shared/Localization/Hoc/sl.json
src/Hoc.Domain.Shared/Localization/Hoc/sv.json
src/Hoc.Domain.Shared/Localization/Hoc/tr.json
src/Hoc.Domain.Shared/Localization/Hoc/vi.json
src/Hoc.Domain.Shared/Localization/Hoc/zh-Hans.json
src/Hoc.Domain.Shared/Localization/Hoc/zh-Hant.json
src/Hoc.Domain.Shared/Localization/HocResource.cs
src/Hoc.Domain.Shared/MultiTenancy/MultiTenancyConsts.cs
src/Hoc.Domain/Data/HocDbMigrationService.cs
src/Hoc.Domain/Data/IHocDbSchemaMigrator.cs
src/Hoc.Domain/Data/NullHocDbSchemaMigrator.cs
src/Hoc.Domain/HocConsts.cs
src/Hoc.Domain/HocDomainModule.cs
src/Hoc.Domain/OpenIddict/OpenIddictDataSeedContributor.cs
src/Hoc.Domain/Properties/AssemblyInfo.cs
src/Hoc.Domain/Settings/HocSettingDefinitionProvider.cs
src/Hoc.Domain/Settings/HocSettings.cs
src/Hoc.EntityFrameworkCore/EntityFrameworkCore/EntityFrameworkCoreHocDbSchemaMigrator.cs
src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocDbContext.cs
src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocDbContextFactory.cs
src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocEfCoreEntityExtensionMappings.cs
src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocEntityFrameworkCoreModule.cs
src/Hoc.EntityFrameworkCore/Migrations/20261102145512_InitialCreate.Designer.cs
src/Hoc.EntityFrameworkCore/Migrations/20261102145512_InitialCreate.cs
src/Hoc.EntityFrameworkCore/Migrations/HocDbContextModelSnapshot.cs
src/Hoc.EntityFrameworkCore/Properties/AssemblyInfo.cs
src/Hoc.HttpApi.Client/HocHttpApiClientModule.cs
src/Hoc.HttpApi/Controllers/HocController.cs
src/Hoc.HttpApi/HocHttpApiModule.cs
src/Hoc.HttpApi/Models/Test/TestModel.cs
src/Hoc.Web/Components/_ViewImports.cshtml
src/Hoc.Web/HocBrandingProvider.cs
src/Hoc.Web/HocWebMappers.cs
src/Hoc.Web/HocWebModule.cs
src/Hoc.Web/Menus/HocMenuContributor.cs
src/Hoc.Web/Menus/HocMenus.cs
src/Hoc.Web/Pages/HocPageModel.cs
src/Hoc.Web/Pages/Index.cshtml
src/Hoc.Web/Pages/Index.cshtml.cs
src/Hoc.Web/Pages/Index.css
src/Hoc.Web/Pages/Index.js
src/Hoc.Web/Pages/_ViewImports.cshtml
src/Hoc.Web/Program.cs
src/Hoc.Web/Properties/AssemblyInfo.cs
src/Hoc.Web/Properties/launchSettings.json
src/Hoc.Web/Views/_ViewImports.cshtml
src/Hoc.Web/abp.resourcemapping.js
src/Hoc.Web/appsettings.Development.json
src/Hoc.Web/appsettings.json
src/Hoc.Web/web.config
src/Hoc.Web/wwwroot/global-styles.css
test/Hoc.Application.Tests/HocApplicationTestBase.cs
test/Hoc.Application.Tests/HocApplicationTestModule.cs
test/Hoc.Application.Tests/Samples/SampleAppServiceTests.cs
test/Hoc.Domain.Tests/HocDomainTestBase.cs
test/Hoc.Domain.Tests/HocDomainTestModule.cs
test/Hoc.Domain.Tests/Samples/SampleDomainTests.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/Applications/EfCoreSampleAppServiceTests.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/Domains/EfCoreSampleDomainTests.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreCollection.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreCollectionFixtureBase.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreFixture.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreTestBase.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreTestModule.cs
test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/Samples/SampleRepositoryTests.cs
test/Hoc.HttpApi.Client.ConsoleTestApp/ClientDemoService.cs
test/Hoc.HttpApi.Client.ConsoleTestApp/ConsoleTestAppHostedService.cs
test/Hoc.HttpApi.Client.ConsoleTestApp/HocConsoleApiClientModule.cs
test/Hoc.HttpApi.Client.ConsoleTestApp/Program.cs
test/Hoc.HttpApi.Client.ConsoleTestApp/appsettings.json
test/Hoc.TestBase/HocTestBase.cs
test/Hoc.TestBase/HocTestBaseModule.cs
test/Hoc.TestBase/HocTestConsts.cs
test/Hoc.TestBase/HocTestDataSeedContributor.cs
test/Hoc.TestBase/Security/FakeCurrentPrincipalAccessor.cs
test/Hoc.Web.Tests/HocWebTestBase.cs
test/Hoc.Web.Tests/HocWebTestModule.cs
test/Hoc.Web.Tests/Pages/Index_Tests.cs
test/Hoc.Web.Tests/Program.cs
test/Hoc.Web.Tests/xunit.runner.json
```

#### Third-party static assets copied into candidate source (14)
Origin: No file bears an attribution header. Five PNGs and nine SVGs are committed candidates; the exact bytes were not found in the restored current node_modules graph. Their filenames, logos, and social-service marks are not license evidence.
License evidence: None established for these exact bytes. `@fortawesome/fontawesome-free` 7.3.1 is present only in ignored node_modules and declares `(CC-BY-4.0 AND OFL-1.1 AND MIT)`; the candidate SVG hashes did not match it, so its terms must not be applied to these files.
Publication disposition: EXCLUDE OR BLOCK. Simple omission would leave live image references in `Pages/Index.cshtml` and `wwwroot/global-styles.css`; a separately reviewed UI replacement/removal is needed before they can be excluded without degrading the starter.
```text
src/Hoc.Web/wwwroot/images/getting-started/bg-01.png
src/Hoc.Web/wwwroot/images/getting-started/book.png
src/Hoc.Web/wwwroot/images/getting-started/discord.svg
src/Hoc.Web/wwwroot/images/getting-started/img-blog.png
src/Hoc.Web/wwwroot/images/getting-started/img-community.png
src/Hoc.Web/wwwroot/images/getting-started/img-support.png
src/Hoc.Web/wwwroot/images/getting-started/instagram.svg
src/Hoc.Web/wwwroot/images/getting-started/stack-overflow.svg
src/Hoc.Web/wwwroot/images/getting-started/x-white.svg
src/Hoc.Web/wwwroot/images/getting-started/youtube.svg
src/Hoc.Web/wwwroot/images/logo/leptonx/logo-dark-thumbnail.png
src/Hoc.Web/wwwroot/images/logo/leptonx/logo-dark.png
src/Hoc.Web/wwwroot/images/logo/leptonx/logo-light-thumbnail.png
src/Hoc.Web/wwwroot/images/logo/leptonx/logo-light.png
```

#### Dependency declarations and resolution locks (33)
Origin: Project/NuGet/Yarn configuration generated or maintained for the starter.
License evidence: Package-specific metadata in 15 packages.lock.json files and src/Hoc.Web/yarn.lock. They reference 306 distinct resolved external NuGet ID/version pairs after project nodes are omitted; no package binaries are candidates.
Publication disposition: SOURCE-ONLY REFERENCE. They may be published only with accurate provenance/attribution documentation; they do not themselves authorize republishing the resolved binaries or client assets.
```text
NuGet.Config
src/Hoc.Application.Contracts/Hoc.Application.Contracts.csproj
src/Hoc.Application.Contracts/packages.lock.json
src/Hoc.Application/Hoc.Application.csproj
src/Hoc.Application/packages.lock.json
src/Hoc.DbMigrator/Hoc.DbMigrator.csproj
src/Hoc.DbMigrator/packages.lock.json
src/Hoc.Domain.Shared/Hoc.Domain.Shared.csproj
src/Hoc.Domain.Shared/packages.lock.json
src/Hoc.Domain/Hoc.Domain.csproj
src/Hoc.Domain/packages.lock.json
src/Hoc.EntityFrameworkCore/Hoc.EntityFrameworkCore.csproj
src/Hoc.EntityFrameworkCore/packages.lock.json
src/Hoc.HttpApi.Client/Hoc.HttpApi.Client.csproj
src/Hoc.HttpApi.Client/packages.lock.json
src/Hoc.HttpApi/Hoc.HttpApi.csproj
src/Hoc.HttpApi/packages.lock.json
src/Hoc.Web/Hoc.Web.csproj
src/Hoc.Web/package.json
src/Hoc.Web/packages.lock.json
src/Hoc.Web/yarn.lock
test/Hoc.Application.Tests/Hoc.Application.Tests.csproj
test/Hoc.Application.Tests/packages.lock.json
test/Hoc.Domain.Tests/Hoc.Domain.Tests.csproj
test/Hoc.Domain.Tests/packages.lock.json
test/Hoc.EntityFrameworkCore.Tests/Hoc.EntityFrameworkCore.Tests.csproj
test/Hoc.EntityFrameworkCore.Tests/packages.lock.json
test/Hoc.HttpApi.Client.ConsoleTestApp/Hoc.HttpApi.Client.ConsoleTestApp.csproj
test/Hoc.HttpApi.Client.ConsoleTestApp/packages.lock.json
test/Hoc.TestBase/Hoc.TestBase.csproj
test/Hoc.TestBase/packages.lock.json
test/Hoc.Web.Tests/Hoc.Web.Tests.csproj
test/Hoc.Web.Tests/packages.lock.json
```

#### Local IDE artifact (1)
Origin: JetBrains solution settings; not necessary to restore, build, or run the documented starter.
License evidence: No third-party license issue is asserted; it is workspace-local configuration.
Publication disposition: EXCLUDE from the first commit. It is currently unignored, so exclusion requires the final named-path allowlist (or a separately approved ignore rule); do not stage it.
```text
Hoc.sln.DotSettings
```

### Third-party source and copied-content result

No independently identifiable third-party C# or JavaScript source file is present outside the ABP-template-output category and the 14 static assets above. That is a limited inspection result: absent a template manifest or byte-for-byte upstream sources, it does **not** prove that every generated C#/JSON/README line is original or that the generated output is outside the scope of upstream license obligations.

### Version-specific license and attribution matrix

| Component / material | Exact version or identity | Evidence URL and inspected metadata | Distribution state in this candidate | Required treatment / status |
| --- | --- | --- | --- | --- |
| ABP Framework and referenced application modules | 141 restored `Volo.Abp.*` packages, 10.7.0 | [ABP 10.7.0 release](https://github.com/abpframework/abp/releases/tag/10.7.0), [ABP LICENSE at 10.7.0](https://github.com/abpframework/abp/blob/10.7.0/LICENSE.md), and local 10.7.0 nuspecs (all `LGPL-3.0-only`) | Referenced through project/lock files; no NuGet binaries in candidate | Preserve applicable LGPL notices/license text and determine source/corresponding-source obligations for template-derived files before publication. **UNRESOLVED.** |
| Direct ABP application modules | Account, Identity, PermissionManagement, TenantManagement, FeatureManagement, SettingManagement, OpenIddict, AuditLogging, BackgroundJobs, EF Core, HttpApi, UI, and test modules at 10.7.0 as listed in the candidate `.csproj` files | Same local nuspec family and ABP license evidence above | Referenced; their APIs are used by the generated app. Not bundled as binaries | Do not treat a package reference as Học-owned source or as a right to redistribute package payloads. **UNRESOLVED as to template-output compliance.** |
| ABP template-generated source | Candidate application/test/template files listed in the manifest | ABP repository is LGPL-3.0; the workspace lacks the exact template archive/revision/manifest that maps each output file | Source output is redistributed if committed | Retain provenance and comply with any applicable LGPL notice/source conditions. Per-file exact origin is not established. **CRITICAL UNKNOWN.** |
| LeptonX Lite .NET theme | `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` 5.7.0 | [NuGet 5.7.0 package page](https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite/5.7.0); local nuspec has no `license` element or bundled license; repository field is incomplete (`https://github.com/abpio/`) | Referenced only by `src/Hoc.Web/Hoc.Web.csproj`; its DLL/content is not a candidate | Obtain authoritative 5.7.0 terms and any required notice before distributing its payload or assets. **UNRESOLVED.** |
| LeptonX Lite npm package | `@abp/aspnetcore.mvc.ui.theme.leptonxlite` 5.7.0, Yarn integrity `sha512-sDRM...YPKQ==` | [npm package](https://www.npmjs.com/package/@abp/aspnetcore.mvc.ui.theme.leptonxlite/v/5.7.0); restored package metadata says `LGPL-3.0`, repository `volosoft/lepton` | Declared in package.json/yarn.lock; restored node_modules is ignored | If restored assets are ever copied into the repository or a release artifact, retain exact license/notice text and reconcile terms with the NuGet package. **UNRESOLVED.** |
| Actual restored client bundles | `wwwroot/libs/**` and `node_modules/**`; including Font Awesome 7.3.1 in current node_modules | Local Font Awesome package metadata: `(CC-BY-4.0 AND OFL-1.1 AND MIT)`; all restored bundle directories are ignored | Not a candidate and not examined as a publication payload | Do not publish from this checkout without a separate asset inventory, exact notices, font attribution, and term review. **EXCLUDED.** |
| Candidate copied static assets | 14 files under `src/Hoc.Web/wwwroot/images/**` | No authoritative source, copyright notice, or license for exact bytes found. Candidate SVGs do not hash-match current restored Font Awesome SVGs. | Redistributed directly if committed | Exclude only with a separately reviewed UI change, or obtain authoritative per-asset provenance/terms. **CRITICAL BLOCKER.** |
| NuGet dependency graph | 15 `packages.lock.json`; 306 distinct external ID/version pairs after project nodes omitted | Lock files are candidate evidence; package metadata is package-specific. Earlier scan categories include LGPL-3.0-only, MIT, Apache-2.0, BSD, PostgreSQL, and file/no-expression records. | Integrity/declaration metadata only, not object code | Keep locks; do not assert a blanket license. Collect exact texts/notices for a future binary or vendored distribution. **SOURCE-ONLY; not legal clearance.** |
| Yarn dependency graph | `src/Hoc.Web/package.json`, `src/Hoc.Web/yarn.lock` | Yarn registry URLs and integrity hashes in lock file | Dependency declaration only, no node_modules files | Keep as dependency provenance; assets installed later remain a separate redistribution event. **SOURCE-ONLY.** |

### LICENSE, NOTICE, and THIRD-PARTY-NOTICES validation

| File | Result | Evidence-based finding |
| --- | --- | --- |
| `LICENSE` | PARTIAL | It is the verbatim Apache-2.0 text, but a license text alone does not attest who owns Học-authored material. It neither relicenses ABP nor resolves the static assets. |
| `NOTICE` | PARTIAL | Its limited scope is accurate. It does not contain an ABP/LGPL license copy or a verified upstream attribution list, so it is not sufficient evidence for a concluded LGPL template-source distribution path. |
| `THIRD-PARTY-NOTICES.md` | PARTIAL | Its distinctions between Học material, ABP, LeptonX, locks, and ignored restored assets are materially accurate. It is incomplete for this exact candidate because it cannot supply authoritative provenance or redistribution terms for the 14 committed assets or an exact template-output provenance record. |

No notice was altered: the authoritative evidence supports the existing cautious wording, but not a new affirmative permission statement. In particular, no Apache-2.0 label was applied to ABP, LeptonX, Font Awesome, logos, social marks, or copied imagery.

### Excluded/private artifact verification

`git check-ignore -v` verified these controls: `.tools/**`; all `bin/**` and `obj/**`; `appsettings.secrets.json`; `*.pfx`, `*.p12`, `*.pem`, `*.key`, `*.crt`, `*.cer`, `*.snk`; `*.db`, `*.sqlite`, `*.sqlite3`, `*.bak`; `Logs/**`; `node_modules/**`; and `wwwroot/libs/**`. The ignored state contains the local PostgreSQL/tool cache, local secret overlays, build outputs, logs, restored libraries, and node modules. No candidate file had an embedded NUL byte except the nine listed PNG assets. The redacted sensitive-pattern scan found only code/configuration identifiers in migration/seed/configuration logic, not a literal credential value.

`Hoc.sln.DotSettings` is the sole reviewed local artifact still unignored; exclude it from the named first-commit allowlist. A final allowlist must also exclude every ignored path above, all runtime publish outputs, and any future overlay/certificate/database file regardless of whether it happens to be present now.

### Focused production-certificate fail-closed verification

**Static result: PASS.** `src/Hoc.Web/HocWebModule.cs` lines 88-105 disable development encryption/signing certificates outside Development, read `OpenIddict:CertificatePassword`, and throw `InvalidOperationException` before adding the production certificate when the value is null/whitespace. The message explicitly requires configuration outside tracked source.

**Bounded negative runtime attempt: BLOCKED BEFORE HOST START.** One attempt set only `ASPNETCORE_ENVIRONMENT=Production` and invoked the existing single `Hoc.Pages.Index_Tests.Welcome_Page` test with `--no-restore -m:1`; no real certificate was supplied and no production service was started. The .NET host rejected the command before compilation/test-host creation because `global.json` requires SDK 10.0.403 while the machine exposes only 10.0.400-preview.0.26322.102. No retry, SDK installation, global.json change, service launch, or alternate target was attempted. Therefore the runtime throw is **UNVERIFIED**, not a passing test.

### Resolved findings, critical unknowns, and disposition

| Finding | Status | Evidence / consequence |
| --- | --- | --- |
| Public candidate has no package binaries, restored web libraries, tool cache, local secret overlay, certificate/key, database, build output, publish output, or logs | PASS for present paths | Ignore enforcement was checked by named representative paths and ignored-status inventory. |
| `Hoc.sln.DotSettings` is a first-commit candidate | RESOLVED disposition | Exclude via the final named-path allowlist; it is not needed by the starter. |
| Root Apache/NOTICE boundaries avoid relicensing third-party materials | PASS (narrow) | The documents make no unsupported third-party grant. |
| ABP package family license identifier | VERIFIED | 141 local 10.7.0 nuspecs consistently say `LGPL-3.0-only`. This is license evidence, not final compliance advice. |
| LeptonX Lite 5.7.0 .NET redistribution terms | UNRESOLVED | NuGet package supplies no license field/text; npm metadata does not cure the .NET package ambiguity. |
| Template-output exact provenance and LGPL distribution method | CRITICAL UNKNOWN | No exact generator/template mapping or concluded notice/source method exists. |
| 14 copied static assets | CRITICAL BLOCKER | Exact source/terms are unestablished; they remain referenced by the starter UI. |
| Học copyright authority | CRITICAL UNKNOWN | Required before an Apache grant can be relied upon. |
| Non-development certificate fail-closed behavior | STATIC PASS / RUNTIME UNVERIFIED | SDK mismatch blocked the single bounded negative attempt before any host started. |

**Final recommendation: NO-GO.** Do not create `letienhoang/Hoc`, stage, commit, push, publish a package, or deploy. Before a later public-source decision, obtain Owner/copyright attestation; record the exact ABP template provenance and applicable LGPL notice/source approach; obtain authoritative LeptonX Lite 5.7.0 .NET terms; establish per-asset provenance/terms or make a separately reviewed UI replacement/removal; exclude `Hoc.sln.DotSettings`; restore the pinned SDK only under separate authority if runtime certificate evidence is required; and have Technical Lead/legal review approve a named path allowlist. This task stops at the evidence report.

---

## H1-P04 Remaining Publication Blockers — 2026-11-03

**Disposition: NO-GO remains in force.** This addendum supersedes only the H1-P03 conclusion that the 14 image/SVG files lacked an exact upstream origin. It records a read-only comparison against the official `abpframework/abp` `10.7.0` tag, commit `c68e856d06615c6bd512b0649c10a4dc45af33e6`, and local restored package metadata. No application source, static asset, configuration, ignore rule, package reference, lock file, Git index, remote, repository, commit, push, deployment, database, or H1-02 material was changed.

This remains an engineering evidence record, not legal advice or legal clearance.

### Rechecked baseline and SDK

| Check | Result | Evidence / boundary |
| --- | --- | --- |
| Git state | PASS | The repository is still unborn `main`; `HEAD` is unresolved; all candidate paths are untracked and nothing is staged. |
| Candidate count | PASS | `git ls-files --others --exclude-standard` still returns 174 paths. Updating this report did not add a path. |
| SDK pin | PASS | `global.json` specifies `10.0.403`, `rollForward: disable`, and `allowPrerelease: false`; `.tools/dotnet/dotnet.exe --info` selected SDK `10.0.403` from this workspace. |
| Focused build/test | NOT RUN | This addendum changes evidence documentation only. The previously accepted build, migration, and test evidence was not repeated. |

### Asset provenance: exact 10.7.0 source match

For every row below, the local SHA-256 and Git blob object were computed from the candidate byte stream. The corresponding blob at official ABP tag `10.7.0` has the same Git blob object. This is exact-byte evidence, not a visual-similarity inference. The upstream base directory is `templates/app/aspnet-core/src/MyCompanyName.MyProjectName.Web/wwwroot/images/` in [ABP 10.7.0](https://github.com/abpframework/abp/tree/10.7.0/templates/app/aspnet-core).

The authoritative repository-level license is [LGPL-3.0](https://github.com/abpframework/abp/blob/10.7.0/LICENSE.md). The template paths contain no more-specific asset license or attribution file. Conservative source-distribution treatment is therefore to preserve ABP/Volosoft attribution and the LGPL-3.0 text, retain the source/provenance pointer, and not label these files Apache-2.0 or Học-owned. The assets are source-template files, not copies extracted from restored `node_modules` or a NuGet package.

| Candidate path | SHA-256 | Exact official source | Repository reference / UI impact if excluded |
| --- | --- | --- | --- |
| `src/Hoc.Web/wwwroot/images/getting-started/bg-01.png` | `83bc2df110734f577aeabae1072593bfda760d45fc0b2b08e1cf2eb20b8f1d4d` | `.../getting-started/bg-01.png` | `Pages/Index.cshtml:19`; removing it leaves the Getting Started card without its background artwork. |
| `src/Hoc.Web/wwwroot/images/getting-started/book.png` | `0ac464f64106c890918f5e0ce92688d4bb95b5769e59302792d09902c4c97ec9` | `.../getting-started/book.png` | No current repository reference; it may be excluded from a later separately reviewed cleanup without changing the current source render. |
| `src/Hoc.Web/wwwroot/images/getting-started/discord.svg` | `ece8576ad3a3ecb8fea4d6c9b19797cbc961717dce1b5d33507414dee62e47a5` | `.../getting-started/discord.svg` | No current repository reference; same exclusion boundary as `book.png`. |
| `src/Hoc.Web/wwwroot/images/getting-started/img-blog.png` | `5ed457dee62191b4309f04df2a62de8b0f8f934d28134a43a47af40bd2776817` | `.../getting-started/img-blog.png` | `Pages/Index.cshtml:42`; removing it leaves the Blog card without its illustration. |
| `src/Hoc.Web/wwwroot/images/getting-started/img-community.png` | `00fecd7e6628c5c40c19888ef85d717d2717818bcace9651410a60cd4cc19420` | `.../getting-started/img-community.png` | `Pages/Index.cshtml:57`; removing it leaves the Community card without its illustration. |
| `src/Hoc.Web/wwwroot/images/getting-started/img-support.png` | `1818f93bc3576454274dfd97b50f34f291fac19a954929de285f478a5445e51e` | `.../getting-started/img-support.png` | `Pages/Index.cshtml:32`; removing it leaves the Support card without its illustration. |
| `src/Hoc.Web/wwwroot/images/getting-started/instagram.svg` | `b6826c6acc49b0a7ee44c55557fe7caf52cada69f7d3ec5a2ca5d5a0f75aab02` | `.../getting-started/instagram.svg` | No current repository reference; same exclusion boundary as `book.png`. |
| `src/Hoc.Web/wwwroot/images/getting-started/stack-overflow.svg` | `4728f1f5c3c430c79a90298a225b35b66874a61f27b92a5a00af3cd92db9ebe5` | `.../getting-started/stack-overflow.svg` | No current repository reference; same exclusion boundary as `book.png`. |
| `src/Hoc.Web/wwwroot/images/getting-started/x-white.svg` | `cb9734b439a938c9d345da1b8ac42200651a828ed1bcf76e6ff99188e1ba694c` | `.../getting-started/x-white.svg` | No current repository reference; same exclusion boundary as `book.png`. |
| `src/Hoc.Web/wwwroot/images/getting-started/youtube.svg` | `a8e9b0a73d2b4bb5899716a33e32852e666311d527c896459da125306b5a1d35` | `.../getting-started/youtube.svg` | No current repository reference; same exclusion boundary as `book.png`. |
| `src/Hoc.Web/wwwroot/images/logo/leptonx/logo-dark-thumbnail.png` | `d2b971ef512cec4fbf484d97ec48552f081558f6ca083d4fd769d5b2a70218ef` | `.../logo/leptonx/logo-dark-thumbnail.png` | No current repository reference; a later separately reviewed theme/brand cleanup may exclude it. |
| `src/Hoc.Web/wwwroot/images/logo/leptonx/logo-dark.png` | `b87d813f124c36099dedea3147b18019ee07bf6c6f3fcf197911a8c85af1466c` | `.../logo/leptonx/logo-dark.png` | No current repository reference; a later separately reviewed theme/brand cleanup may exclude it. |
| `src/Hoc.Web/wwwroot/images/logo/leptonx/logo-light-thumbnail.png` | `ee69152fc597d7de54825f89cc34a4d1755a36dbefb6daed85c3cbe67544f934` | `.../logo/leptonx/logo-light-thumbnail.png` | `wwwroot/global-styles.css:5`; removal breaks the configured compact brand logo. |
| `src/Hoc.Web/wwwroot/images/logo/leptonx/logo-light.png` | `f2080171624cef7d90ae9c4479aacbedf4723a49d1d49231273a5ef92098adc7` | `.../logo/leptonx/logo-light.png` | `wwwroot/global-styles.css:4`; removal breaks the configured full brand logo. |

**Asset result: RESOLVED.** The smallest safe action is to retain all 14 with the ABP LGPL attribution/provenance treatment above. No generated or downloaded replacement is needed. The eight source-unreferenced files may instead be excluded only in a separately reviewed deletion/change; that is not authorized or performed by H1-P04.

### ABP template-source comparison and attribution boundary

The official template tree was compared at commit `c68e856d06615c6bd512b0649c10a4dc45af33e6`. For text files, the comparison applies the generator's project-name rendering (`MyCompanyName.MyProjectName` and `MyProjectName` to `Hoc`) and removes template-control directives. This is sufficient to establish the source template relationship, but it does not convert upstream copyright into Học ownership.

| Classification | Exact result | Treatment |
| --- | --- | --- |
| Unchanged generated template output | 107 text files match after the stated deterministic template rendering; 14 static assets match byte-for-byte. Total: **121**. This includes the layered modules, pages, localization, tests, resource mapping, template settings, and the 14 assets above. | Keep ABP provenance. Do not relicense as Apache-2.0 or claim these whole files are Học original. |
| Rendered/modified template output | **30** matching-path files differ from the raw template source. The set is: `.gitignore`, `Hoc.slnx`, `README.md`; all 15 project files; `HocDbMigratorModule.cs`; `appsettings.json` in DbMigrator, Web, and console test app; `HocDbMigrationService.cs`; `OpenIddictDataSeedContributor.cs`; `HocDbContextFactory.cs`; `HocEntityFrameworkCoreModule.cs`; `HocDbContextModelSnapshot.cs`; `HocWebModule.cs`; `package.json`; and `wwwroot/global-styles.css`. Differences are explained by selected MVC/non-tiered/PostgreSQL/LeptonX Lite template rendering, generated package references, migration output, and the documented H1 secret/ignore hardening. | Preserve ABP provenance for the inherited portions. Attribute Học only to the narrow, reviewed additions/edits, not the full files. |
| Locally generated, not ABP template source files | 15 `packages.lock.json` files, `src/Hoc.Web/yarn.lock`, and the two timestamped EF Core migration files are restore/tool outputs for this local solution. The current EF snapshot is also locally generated; it has a template-path counterpart but its serialized content differs after the local PostgreSQL/migration generation. | Do not call these files wholly Học-authored hand-written code. Retain their generator/dependency provenance. |
| Học-authored documentation/control material | `LICENSE`, `NOTICE`, `THIRD-PARTY-NOTICES.md`, this report, `global.json`, and the narrow hardening content in `.gitignore`/`README.md` are locally added or edited material. | **OWNER ACTION:** a copyright holder must attest the intended Apache-2.0 scope. The root Apache text does not relicense any template-derived file, package, logo, or asset. |

The applicable upstream framework/template license is LGPL-3.0. For a conservative public source release, include the unmodified LGPL-3.0 text, retain upstream copyright/license notices and a source/tag pointer, provide the source actually shipped for any LGPL-covered template material, and preserve modification notices where applicable. This record does not decide whether a particular generated output is a derivative work; that legal characterization and any broader corresponding-source obligation require Owner/legal approval. No ABP source is reclassified as Apache-2.0 here.

### LeptonX Lite: package boundary and precise clarification

**FACT:** `src/Hoc.Web/Hoc.Web.csproj` references `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` `5.7.0`. The candidate source set includes that project reference, `package.json`, and `yarn.lock`; it includes **no** restored NuGet DLL, embedded resource extraction, `node_modules`, or `wwwroot/libs` payload. The restored MVC NuGet package does contain a DLL and embedded MVC/theme resources, but those are not candidate paths. ABP documents LeptonX Lite as the default free theme and says the MVC package is installed by startup templates; that product description is not a license grant. See [official MVC documentation](https://abp.io/docs/latest/ui-themes/lepton-x-lite/asp-net-core) and [theme overview](https://abp.io/docs/latest/ui-themes/lepton-x-lite/).

**FACT:** the exact MVC/Razor NuGet package nuspec for version `5.7.0` has no `<license>` element and the restored package has no bundled license file. It names `https://commercial.abp.io/` as `projectUrl` and repository `https://github.com/abpio/` at commit `030ee0436595e7143ff0cc1cd67afbae8485cd23`. Its [NuGet package page](https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite/5.7.0) does not substitute for a license text. The npm package's LGPL declaration is deliberately not used to answer this MVC NuGet question.

**UPSTREAM CLARIFICATION — concise question for Volosoft:**

> Please confirm the license and public-source redistribution terms for `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` **5.7.0** for ASP.NET Core MVC/Razor. May a public repository distribute a source template generated by `abp new --theme leptonx-lite` that retains the template's 14 `wwwroot/images` files and merely references the NuGet package (with no NuGet DLL, embedded resource extraction, `node_modules`, or restored client libraries)? Please provide the authoritative license text, required copyright/notice text, and any conditions. This question is specifically about the MVC/Razor NuGet package, not the Angular npm package.

### Conditional first-publication allowlist and private-file verification

**Exact conditional source-only set: 173 paths.** It is the exhaustive 174-path manifest in the H1-P03 section above, **minus exactly** `Hoc.sln.DotSettings`. This set is path-safe only; it is not an authorization to stage, commit, publish, or grant rights. The 14 assets remain in the set because their exact ABP 10.7.0 source and repository license are now established. No ignored path belongs to the set.

`Hoc.sln.DotSettings` is **EXCLUDED** from the first-publication allowlist. It is unchanged template/IDE material but not needed to restore, build, test, or run the starter. Do not infer a right to share it from this review; an Owner-approved inspection would be required before considering it later.

The ignore policy was rechecked without staging: `.tools/**`, `bin/**`, `obj/**`, `appsettings.secrets.json`, local/production overlays, `*.pfx`, `*.p12`, `*.pem`, `*.key`, `*.crt`, `*.cer`, `*.snk`, local database formats, logs, `node_modules/**`, and `wwwroot/libs/**` remain ignored. The candidate contains no path under those protected locations. Sensitive-pattern review remains limited to path/classification evidence; no credential value is reproduced here.

### Final blocker disposition

| Finding | Final disposition | Evidence / required action |
| --- | --- | --- |
| 14 template image/SVG candidates | **RESOLVED** | All 14 are byte-identical to official ABP 10.7.0 template files; retain LGPL attribution/provenance. |
| ABP template provenance | **RESOLVED** | Official tag/commit comparison establishes the template origin and identifies 121 unchanged outputs plus bounded rendered/modified/generated groups. |
| ABP LGPL treatment | **OWNER ACTION** | Owner/legal must approve the final LGPL notice, attribution, modification-notice, and corresponding-source approach; no source is relicensed here. |
| Học Apache-2.0 scope | **OWNER ACTION** | Copyright holder must attest ownership and the intended scope for Học-authored documentation and narrow modifications. |
| LeptonX Lite MVC/Razor NuGet 5.7.0 terms | **UPSTREAM CLARIFICATION** | Nuspec/license-file absence leaves the precise MVC package/source-template redistribution terms unanswered; send the question above to Volosoft. |
| LeptonX Lite binaries/embedded assets in proposed public files | **RESOLVED** | None are in the 173-path conditional source-only set; only package/dependency declarations remain. |
| `Hoc.sln.DotSettings` | **EXCLUDED** | Omitted from the first-publication allowlist. |
| Ignored/private/generated runtime files | **EXCLUDED** | Ignore verification covers tools, secrets, credentials, certificates, databases, logs, restore trees, build/publish output, and machine-local artifacts. |
| Owner NO-GO and no GitHub write authority | **BLOCKED** | Still binding; no repository, commit, push, publication, deployment, or H1-02 work is authorized. |

**GO/NO-GO: NO-GO.** The asset and template provenance blockers are resolved, and the exact conditional source-only path set is defined. Publication is still blocked by the Owner NO-GO, required Học copyright/Apache scope attestation, Technical Lead/legal approval of the LGPL attribution/source approach, and an authoritative Volosoft answer for the MVC/Razor LeptonX Lite NuGet package. No GitHub write operation or H1-02 activity is authorized by this result.

---

## H1-P05 First Public Commit Preparation — 2026-11-03

**Disposition: NO-GO remains in force.** This documentation-only pass adds
root repository guidance, completes the explicit ABP/LeptonX Lite attribution
wording, and excludes the local JetBrains solution settings. It did not stage,
commit, push, create a repository, contact GitHub, run an application, access a
database, change a production system, or repeat prior build/migration checks.

### Exact conditional first-commit set

The H1-P04 conditional source-only set remains the 173 paths defined by its
exhaustive H1-P03 manifest, minus `Hoc.sln.DotSettings`. This pass adds exactly
one public repository-control file: `AGENTS.md`. Therefore the current exact
conditional first-commit set is **174 paths**: the H1-P04 173 paths plus
`AGENTS.md`. It remains a path-safe allowlist candidate only, not an approval to
stage, commit, publish, or grant rights.

`Hoc.sln.DotSettings` is now explicitly ignored and excluded. Representative
checks also confirm ignored treatment for tools, logs, local and production
configuration overlays, secret overlays, certificates/keys, `node_modules`,
and restored `wwwroot/libs` assets. No path beneath those locations is in the
174-path candidate set.

### Attribution and dependency boundary

- The root `LICENSE` remains the unmodified Apache License 2.0 text. Its scope
  is limited by `NOTICE` to Học-owned material for which authority is attested.
- `NOTICE` and `THIRD-PARTY-NOTICES.md` now identify ABP 10.7.0 template-derived
  source and the 14 exact-byte-matched template assets as LGPL-3.0-only
  provenance. They preserve the ABP/Volosoft attribution and official tag/commit
  pointer without relicensing those files as Apache-2.0.
- LeptonX Lite MVC/Razor 5.7.0 is documented solely as an external NuGet
  dependency. No package payload is included in the source candidate, and no
  redistribution right is asserted pending the upstream clarification.

### Focused checks

| Check | Result | Boundary |
| --- | --- | --- |
| Candidate count | PASS | `git ls-files --others --exclude-standard` returned 174. |
| Local IDE exclusion | PASS | `Hoc.sln.DotSettings` is ignored and not in the candidate set. |
| Representative private/local exclusions | PASS | Named checks passed for the protected categories above. |
| Bounded private-key/token marker scan | PASS | No candidate file matched the checked PEM, GitHub-token, or AWS-key markers. This is not a claim that future changes are secret-free. |
| Documentation whitespace | PASS | `git diff --check` returned zero; no source/build/test validation was repeated because this pass changes only publication guidance and notices. |

### Remaining Owner gates

The Owner NO-GO remains binding. Before a public repository can be created,
obtain Owner copyright/Apache scope attestation, Technical Lead/legal approval
of the LGPL notice/modification/corresponding-source approach, and an
authoritative Volosoft response on the MVC/Razor LeptonX Lite 5.7.0 public-source
redistribution terms. Then review and explicitly stage only the 174 named paths.
