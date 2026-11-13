# Automated Admin module integration v1

`integrate-admin-module.ps1` composes a H4-02 six-project source module into a H4-03 consumer. It automates only the six host composition files; it is not a runtime plugin system, a general installer, or migration tooling.

Run from the Học tooling checkout after H4-03 bootstrap and H4-02 generation:

```powershell
pwsh ./scripts/productization/integrate-admin-module.ps1 -ConsumerRoot C:\work\ContosoAdmin -ApplicationId ContosoAdmin -ModuleName Products -EntityName Product
pwsh ./scripts/productization/integrate-admin-module.ps1 -ConsumerRoot C:\work\ContosoAdmin -ApplicationId ContosoAdmin -ModuleName Products -EntityName Product -Apply
```

The default dry-run validates the consumer, all generated projects/types, safe paths, and supported source shapes, then lists updates. Apply preflights all inputs, updates only `ContosoAdmin.slnx`, Web project/module, EF Core project/module, and DbContext, and restores original bytes if a multi-file write fails. Re-running Apply is a no-op. Ambiguous unsupported source patterns fail closed.

The solution gains Domain, Application.Contracts, Application, EntityFrameworkCore, HttpApi, and Web projects under `/modules/`. The Web host references/dependends on Web and HttpApi; EF Core references/depends on EntityFrameworkCore; the consumer DbContext owns the generated entity `DbSet` and `ConfigureProducts()` mapping call. This preserves ABP dependency direction and makes the consumer—not generated source—the owner of model composition.

Validate with `pwsh ./scripts/productization/test-integrate-admin-module.ps1`. Then use the consumer's pinned SDK to restore and Release-build. Generated H4-02 modules still need consumer-owned migration **source** design and review; migration APPLY, DbMigrator, persistent databases, deployment, and production are excluded.
