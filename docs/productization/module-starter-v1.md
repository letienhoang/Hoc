# Reusable admin module starter v1

## Purpose and boundaries

`scripts/productization/new-admin-module.ps1` creates a consumer-owned, six-project ABP MVC/Razor CRUD module from Học's `samples/Hoc.DemoRecords.*` source reference. It is intended for an independent consumer already derived using [consumer bootstrap v1](consumer-bootstrap-v1.md). The tooling checkout supplies the template; the consumer checkout owns generated code, composition, schema, tests, deployment, and upgrades.

This is deliberately not a universal generator. v1 creates one fixed aggregate: a `Guid` ID, required `Name` (128), and optional `Description` (1024). It has no field designer, arbitrary relationships, host editing, migration generation, migration application, package publishing, runtime loading, or automatic upgrade mechanism.

## Use

Run from the Học tooling checkout. All parameters are required. The default is a dry-run: it checks every precondition and prints the planned directories without changing the consumer.

```powershell
pwsh ./scripts/productization/new-admin-module.ps1 `
  -ConsumerRoot C:\work\ContosoAdmin `
  -ApplicationId ContosoAdmin `
  -ModuleName Products `
  -EntityName Product `
  -RouteSegment products

# Generate only after reviewing the dry-run.
pwsh ./scripts/productization/new-admin-module.ps1 `
  -ConsumerRoot C:\work\ContosoAdmin `
  -ApplicationId ContosoAdmin `
  -ModuleName Products `
  -EntityName Product `
  -RouteSegment products `
  -Apply
```

The consumer root must be a real directory (not a reparse point), contain `ContosoAdmin.slnx` or `ContosoAdmin.sln` and `common.props`, and have no colliding output. IDs are PascalCase identifiers; routes use lower-case letters, digits, and hyphens. The generator validates its own pinned SDK/template baseline, does not access a network or database, rejects path traversal/output escapes/reparse-point redirects, and refuses to overwrite existing output. It completes validation before creating output and removes only directories it created if copying or transformation fails.

For the example, output is:

```text
modules/
  ContosoAdmin.Products.Domain/
  ContosoAdmin.Products.Application.Contracts/
  ContosoAdmin.Products.Application/
  ContosoAdmin.Products.EntityFrameworkCore/
  ContosoAdmin.Products.HttpApi/
  ContosoAdmin.Products.Web/
```

Namespaces and assembly names use `ContosoAdmin.Products`; `Product` identifies the aggregate/DTOs/services/pages; `Products` identifies permissions and menu/localization group; `products` is the supplied API route identity. The generated module preserves the reference's host-only tenant guard, CRUD authorization (`Products.Read`, `.Create`, `.Update`, `.Delete`), conventional HTTP API module, Razor antiforgery validation, permission-protected navigation, and English/Vietnamese resources. It deliberately has no executable `Hoc.DemoRecords` dependency or visible DemoRecords label. ABP/template provenance is retained; consult the repository license and do not treat generated source as a grant to redistribute LeptonX Lite.

## Compose in the consumer

Generation does not mutate a host. In the consumer, explicitly:

1. Add all generated projects to the solution.
2. Add project references from consumer composition projects to the generated Contracts, Application, EF Core, HttpApi, and Web projects as appropriate.
3. Add the generated ABP modules to the matching consumer module `DependsOn` declarations, following the existing dependency direction.
4. Add the entity `DbSet` and call the generated EF Core model configuration from the consumer-owned DbContext model builder.
5. Ensure the consumer HTTP/API and Web hosts depend on the generated modules so API conventions, Razor Pages, and the menu contributor are discovered.
6. Review table naming/indexes and generate migration **source** with the consumer's EF tooling only when separately approved; inspect `Up`, `Down`, and the snapshot.

Do not apply a migration, run DbMigrator, seed a database, or substitute a migration `Down()` for recovery planning. Those remain consumer-owned operations requiring their own authorization and target/recovery evidence.

## Validation and troubleshooting

Run the tool self-tests without touching the checkout or a database:

```powershell
pwsh ./scripts/productization/test-new-admin-module.ps1

# Optional disposable generated-project Release compilation.
pwsh ./scripts/productization/test-new-admin-module.ps1 -CompileGeneratedProjects
```

For a consumer, run dry-run, apply, add the explicit composition above, then use its pinned SDK to restore, Release-build, and execute focused non-persistent tests. H4-02 records only local self-test/source evidence unless a specific disposable consumer exercise is reported alongside its SHA. PostgreSQL, browser, deployed runtime, production behavior, authentication configuration, and arbitrary consumer compatibility are not implied.

Typical failures are intentional safeguards: add the expected consumer solution/common props before running; choose non-existing module names; use valid PascalCase identities/lowercase route; replace a broken or modified tooling template checkout rather than bypassing the baseline check. Review [compatibility v1](compatibility-v1.md) before using a newer source/dependency revision.
