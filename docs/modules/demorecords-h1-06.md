# H1-06 DemoRecords Admin UI

The DemoRecords Web module owns the Razor Pages at `/DemoRecords`,
`/DemoRecords/Create`, `/DemoRecords/Details/{id:guid}`,
`/DemoRecords/Edit/{id:guid}`, and `/DemoRecords/Delete/{id:guid}`.  The
composition host continues to reference the module and does not contain feature
pages.

Each page calls only `IDemoRecordAppService`.  Read, create, update, and delete
pages use their corresponding `DemoRecords.*` permission; the UI also rejects
tenant contexts before calling the service, preserving the application-service
host-only guard as the final authority.  Create, edit, and delete use ordinary
POST forms with MVC antiforgery validation.

`DemoRecordsMenuContributor` contributes one Main-menu item for `/DemoRecords`
with the `DemoRecords.Read` requirement.  It is therefore omitted by ABP menu
rendering for anonymous, unauthorized, and tenant principals.

Validation is TestServer/SQLite evidence only.  It does not demonstrate browser,
PostgreSQL, or deployed-runtime behavior.
