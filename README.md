# Học

Học is a developer-oriented ABP 10 MVC/Razor foundation: a layered modular monolith with PostgreSQL/EF Core, Identity, OpenIddict, permissions, auditing, localization, and the LeptonX Lite theme. `DemoRecords` is the repository's reference sample feature; it is not a required feature of an independently derived consumer.

This repository is source-first. It does not publish a Học runtime package, consumer generator, or automatic upgrade channel. To derive an independent Admin consumer from a pinned source revision, follow the [consumer bootstrap procedure](docs/productization/consumer-bootstrap-v1.md) and its [compatibility policy](docs/productization/compatibility-v1.md).

To add a fixed-shape six-project MVC/Razor CRUD module to an already-derived consumer, use the [admin module starter v1](docs/productization/module-starter-v1.md). It is source-based, dry-run by default, and leaves consumer composition and migration work explicit.

## Prerequisites

- The exact .NET SDK in [global.json](global.json): `10.0.403`.
- Node.js 20.11 or later when restoring MVC client libraries.
- PostgreSQL tooling only for a separately authorized database operation.

On Windows, use `./.tools/dotnet/dotnet.exe` when the repository-local SDK is present. The application uses ABP 10.7 and LeptonX Lite 5.7.0; see the compatibility policy before changing either baseline.

## Existing Học development setup

Development hosts and EF design-time tooling use the ignored workspace overlay `.tools/config/development.secrets.json`. It must contain a complete PostgreSQL connection string with `Ssl Mode=VerifyFull`. The configuration rules, overlay precedence, and fail-closed behavior are documented in [development configuration](docs/development-configuration.md). Do not put a real connection string, token, password, or certificate password in tracked configuration.

Client libraries are generated and ignored. From `src/Hoc.Web`, restore them with the pinned ABP tooling (`abp install-libs`) when they are missing. The CI workflow shows the exact clean-runner sequence.

## Build and test versus database initialization

Restore, build, and non-persistent tests do not initialize a database:

```powershell
dotnet restore Hoc.slnx -m:1 /nr:false
dotnet build Hoc.slnx -c Release --no-restore -m:1 /nr:false
dotnet test test/Hoc.Web.Tests/Hoc.Web.Tests.csproj -c Release --no-build --no-restore -m:1 /nr:false
```

`Hoc.DbMigrator` is deliberately excluded from that workflow. It can apply migrations **and seed persistent data**. Never run it, run a migration, or connect to a persistent target without a separately bounded Owner GO, verified target/TLS, and the recovery evidence required by the engineering workflow.

## Security, governance, and third-party material

- [Engineering workflow](docs/engineering-workflow-v2.md) defines authorization and evidence boundaries.
- [Development configuration](docs/development-configuration.md) defines the secret-overlay and TLS guardrails.
- [Security documentation](docs/security) records Học security decisions.
- [LICENSE](LICENSE) and ABP/third-party notices in their retained source locations govern reuse. ABP-derived material remains LGPL-3.0-only where applicable. LeptonX Lite is an external NuGet dependency: do not vendor, extract, or claim redistribution rights.

MSBuild package metadata is not a public product release statement.
