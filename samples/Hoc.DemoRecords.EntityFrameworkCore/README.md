# DemoRecords persistence ownership

`Hoc.DemoRecords.Domain` owns the `DemoRecord` aggregate and its validation
limits. `Hoc.DemoRecords.EntityFrameworkCore` owns the EF Core table,
property, and index configuration.

`Hoc.EntityFrameworkCore` references the module EF Core project, depends on
its ABP module, exposes `DemoRecords` on `HocDbContext`, and invokes
`ConfigureDemoRecords()` while building the central model. EF migrations and
the snapshot remain in `src/Hoc.EntityFrameworkCore/Migrations`, so the schema
is deployed through the application's existing central migration strategy.

The dependency direction is deliberately acyclic:

```text
Hoc.EntityFrameworkCore -> Hoc.DemoRecords.EntityFrameworkCore -> Hoc.DemoRecords.Domain
Hoc.Web -> Hoc.DemoRecords.Web -> Hoc.DemoRecords.Application.Contracts
```

The expected H1-04 schema delta is one `DemoRecords` table with a `Guid`
primary key, required `Name` (`128`), optional `Description` (`1024`), and a
non-unique index on `Name`. No Identity, OpenIddict, tenant-management, or
other ABP table changes are intended.

Verification uses domain tests and the existing disposable SQLite EF Core test
host. PostgreSQL connectivity, applying the migration, browser acceptance,
and deployed-runtime verification remain outside this foundation scope.
