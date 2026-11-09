# DemoRecords H1-05 application and HTTP API

`Hoc.DemoRecords.Application.Contracts` owns the public DTOs, validated inputs,
permission names, and `IDemoRecordAppService`. `Hoc.DemoRecords.Application`
implements the service over `IRepository<DemoRecord, Guid>`. The HTTP API
module exposes that application assembly through ABP conventional controllers,
and `Hoc.Web` composes the HTTP API module without moving implementation into
the host.

## Routes

The TestServer route inventory verifies these conventional-controller routes:

| Verb | Route | Application operation | Permission |
| --- | --- | --- | --- |
| GET | `/api/demo-records/demo-record/{id}` | `GetAsync` | `DemoRecords.Read` |
| GET | `/api/demo-records/demo-record` | `GetListAsync` | `DemoRecords.Read` |
| POST | `/api/demo-records/demo-record` | `CreateAsync` | `DemoRecords.Create` |
| PUT | `/api/demo-records/demo-record/{id}` | `UpdateAsync` | `DemoRecords.Update` |
| DELETE | `/api/demo-records/demo-record/{id}` | `DeleteAsync` | `DemoRecords.Delete` |

`GetListAsync` accepts `skipCount` and `maxResultCount`; the maximum page size
is 100. Results are deterministically ordered by name and then identifier.

## Security boundary

Each application-service method has an ABP permission authorization attribute.
Before any repository read or write, the service rejects requests when either
`ICurrentTenant.Id` or `ICurrentUser.TenantId` is non-null. It does not change
the tenant context. This protects the host-level `DemoRecords` table even when
a tenant principal has otherwise sufficient temporary test permissions.

The TestServer suite restores ABP's real authorization and permission-checking
services instead of the broad test-base allow-all default. It exercises valid
antiforgery tokens for authenticated mutations, so a CSRF result is not used as
permission evidence. Permission grants and tenant/user data are created only in
the disposable SQLite fixture.

## Validation and persistence

Inputs reject whitespace-only names, names longer than 128 characters,
descriptions longer than 1024 characters, invalid pagination, empty identifiers,
and null request bodies through standard ABP validation/error handling. No EF
mapping, schema, migration, seed data, or persistent database operation is part
of H1-05.
