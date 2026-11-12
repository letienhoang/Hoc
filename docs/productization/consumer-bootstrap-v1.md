# Consumer bootstrap v1

This is a deterministic, manual source-based procedure for deriving an independent Admin consumer. It is a handoff procedure, not a generator, product CLI, or authorization to initialize a database. Static validation cannot prove authentication, PostgreSQL connectivity, migration correctness, browser behavior, or production readiness.

## Baseline and tools

Select one Học commit before copying. H4-01 uses consumer source commit `6a8605a63d18d35767f520d57d33193f6d15aa29`, with `global.json` SDK `10.0.403`, ABP `10.7.0`, and LeptonX Lite `5.7.0`. The read-only validator is taken from the separate Học tooling checkout at H4-01 tooling commit `cd7c8c06b5e8f35e5b9b4d02eef5380fe80a1adc`. Record different source and tooling SHAs if later revisions are intentionally selected; do not call them compatible until they are reviewed under the compatibility policy.

Use the pinned SDK. Node.js 20.11+ is needed only to restore web client assets. No database, secret, certificate, or network connection is required for the structural validator.

## A. Acquire an isolated source copy

1. Clone or copy the selected Học revision into a new, empty consumer folder.
2. Record the upstream repository URL and exact source SHA in the consumer's own documentation.
3. Create a new local Git history for the consumer. Remove inherited remotes before the first consumer commit; add a consumer remote only after verifying its destination cannot overwrite Học.
4. Preserve `LICENSE` and applicable retained ABP/third-party notices. Do not copy NuGet cache contents, generated client libraries, certificates, or secret overlays.

Example, with a synthetic identity:

```powershell
git clone --no-checkout https://github.com/letienhoang/Hoc.git C:\work\ContosoAdmin
Set-Location C:\work\ContosoAdmin
git checkout 6a8605a63d18d35767f520d57d33193f6d15aa29
git remote remove origin
git checkout --orphan consumer-initial
git read-tree --empty
git add -- .gitattributes .gitignore common.props global.json Hoc.slnx LICENSE NOTICE NuGet.Config README.md THIRD-PARTY-NOTICES.md docs src samples test
git diff --cached --name-status
git diff --cached --check
git commit -m "chore: establish ContosoAdmin consumer baseline"
```

`git read-tree --empty` clears the inherited index while leaving the working tree
unchanged. The subsequent explicit path list retains the build and package
inputs (`common.props`, `global.json`, `Hoc.slnx`, and `NuGet.Config`), source
control policy (`.gitattributes` and `.gitignore`), and reviewed license and
notice files (`LICENSE`, `NOTICE`, and `THIRD-PARTY-NOTICES.md`) alongside the
consumer source and documentation. Review other root files before adding them;
do not stage `appsettings.secrets.json`, `.tools`, logs, reports, or generated
client/build output. Never run broad staging blindly.

## B. Establish one application identity

Choose an internal identifier such as `ContosoAdmin`, separately from the consumer-facing product name. Rename solution and project filenames, assemblies, namespaces, root namespaces, localization resource name/class, Web display name, Swagger labels, test names, and all solution/project references together. A consumer must not retain a `Hoc` root namespace or a `Hoc.*` assembly identity.

Rename the OpenIddict audience/scope and configuration identities together:

- audience and resource: `ContosoAdmin`;
- web client: `ContosoAdmin_Web`;
- Swagger client: `ContosoAdmin_Swagger`;
- each client ID, redirect URI, post-logout redirect URI, `App:SelfUrl`, and any development launch URL.

Review the generated redirect URIs exactly: web uses `{RootUrl}/signin-oidc` and `{RootUrl}/signout-callback-oidc`; Swagger uses `{RootUrl}/swagger/oauth2-redirect.html`. Use a consumer-owned database name and local connection configuration. Branding text alone is not an identity rename.

## C. Remove the DemoRecords sample from runtime composition

`samples/Hoc.DemoRecords.*` is Học reference material. Reusable infrastructure is the remaining layered ABP host: Identity, tenant management, authorization, OpenIddict, auditing, localization, EF Core, HTTP API, MVC/Razor, and the theme. Do not delete files merely because a filename contains a word.

Remove every DemoRecords project from the solution, direct project references, `DependsOn` module declarations, EF `DbSet`/mapping, navigation/API composition, and inherited DemoRecords-specific tests in **all** test projects (including Domain, EF Core, and Web). Then review project files and executable source for stale `Hoc.DemoRecords`, `DemoRecords`, and `Hoc` namespace/assembly references. Keep historical provenance and license text out of this mechanical stale-reference rule.

## D. Establish independent persistence

The consumer owns its DbContext, PostgreSQL schema, and migration history. Do not carry Học's `InitialCreate`, `DemoRecordsPersistence`, or Học model snapshot into a new consumer: they describe Học history, including the removed sample.

For a genuinely new consumer database, later and under a separate Owner GO:

1. finish the consumer model and mapping;
2. use the consumer EntityFrameworkCore project as both EF tooling and startup project (the Học convention is intentionally not `DbMigrator`);
3. create one fresh initial migration source;
4. review its `Up`, `Down`, and model snapshot; and
5. separately verify target, TLS, migration history, recovery, and seed behavior immediately around any authorized APPLY.

Generating migration source is distinct from applying it. H4-01 does neither; `DbMigrator` can migrate and seed and is never an ordinary build/test command.

## E. Configure securely

Retain the established Development behavior in [development configuration](../development-configuration.md): the ignored `.tools/config/development.secrets.json` overlay is authoritative for a valid Development PostgreSQL connection string and must use `Ssl Mode=VerifyFull`. Host-local `appsettings.secrets.json` overlays remain ignored and must not silently replace the Development connection target. Missing or malformed Development configuration must fail closed.

Keep real passwords, client secrets, tokens, and certificate passwords out of tracked JSON. `openiddict.pfx` and private keys are local/external secrets; production certificate provisioning is Owner-controlled. Select `DOTNET_ENVIRONMENT=Development` explicitly for design-time work. Do not use a tracked localhost template as a reason to connect, migrate, or seed.

## F. Validate and hand off

Run structural validation before restore. Run the validator from a separate, clean Học tooling checkout containing the recorded tooling SHA; do not expect the validator script to exist in the consumer checkout:

```powershell
$consumerRoot = 'C:\work\ContosoAdmin'
$hocToolingRoot = 'C:\work\Hoc-H4-01-tooling'
Set-Location $hocToolingRoot
pwsh ./scripts/productization/verify-consumer.ps1 `
  -ConsumerRoot $consumerRoot -ApplicationId ContosoAdmin
Set-Location $consumerRoot
```

The validator is read-only: it has no network, database, migration, host-start, or automatic-renaming behavior. It checks solution/project identity, scoped stale sample references, inherited migration artifacts, consumer OpenIddict identity, inspectable SDK/package baselines, and known secret/certificate artifacts. A static PASS is not runtime proof.

For a renamed consumer, restore and build with its own solution name:

```powershell
dotnet restore ContosoAdmin.slnx -m:1 /nr:false
dotnet build ContosoAdmin.slnx -c Release --no-restore -m:1 /nr:false
dotnet test test/ContosoAdmin.Web.Tests/ContosoAdmin.Web.Tests.csproj -c Release --no-build --no-restore -m:1 /nr:false
```

Restore client libraries first when Web/TestServer needs ignored ABP assets. Stop handoff for incomplete identity renames, stale runtime references, inherited/conflicting migrations, secret/certificate artifacts, package or SDK drift, missing required project files, or any need to change Học architecture.

## H4-01 temporary exercise record

H4-01 exercised this procedure with synthetic `ContosoAdmin`, copied from
consumer source commit `6a8605a63d18d35767f520d57d33193f6d15aa29`, using a
separate Học tooling checkout. Start/end were
`2026-11-12T10:14:21+07:00` / `2026-11-12T10:20:14+07:00`, for 5 minutes 53
seconds including rework. The temporary consumer had an independent local Git
root commit (`f164466ec599432772ffd3acffa6eafc805d6f26`) and no remote.

That historical exercise predates tooling commit
`cd7c8c06b5e8f35e5b9b4d02eef5380fe80a1adc`; no original tooling SHA is inferred
for it. The later reproducible procedure separately pins that tooling commit.

Manual work performed: renamed solution/projects/namespaces/configuration to
`ContosoAdmin`; removed DemoRecords runtime references, sample projects,
inherited migrations, and inherited sample tests; made ignored secret overlays
optional; regenerated package locks during restore; ran the structural validator;
and restored ignored ABP client assets with ABP CLI 10.7.0.

Observed rework: the first build exposed Domain-level DemoRecords tests that
had not yet been excluded (10 missing-`DemoRecord` compile errors). The first
Web TestServer attempt then exposed the missing ignored
`/libs/abp/core/abp.css` asset. After excluding those inherited tests and
running `abp install-libs`, restore and Release build succeeded (three warnings
in the temporary copy) and Web TestServer passed 18/18 tests. This is local
source/TestServer evidence only; no database, migration, host, browser, or
production operation occurred.

Future automation opportunity: a narrowly scoped rename/removal checklist can
reduce mechanical work, but H4-01 intentionally does not introduce a generator
or automatic migration/upgrade tool.
