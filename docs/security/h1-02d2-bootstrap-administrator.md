# H1-02D2 Bootstrap Administrator configuration

`Hoc.DbMigrator` requires these configuration keys before it invokes a schema
migrator, any seeder, or the migration-creation helper:

- `Bootstrap:Administrator:Email`
- `Bootstrap:Administrator:Password`

Set them in the ignored `src/Hoc.DbMigrator/appsettings.secrets.json` overlay
or an approved external configuration provider. Do not put either value in
tracked `appsettings.json`, documentation, command history, test output, or
logs. Environment providers use the standard double-underscore form of each
key, for example `Bootstrap__Administrator__Email`.

Both keys are mandatory. The email must be syntactically valid. The password
must be nonblank, meet the configured ASP.NET Core Identity password options,
and must not equal ABP's template-default Administrator password. Invalid or
absent configuration stops the migrator before migration or seeding work and
uses a generic exception that does not include either value.

The validated values are passed through ABP's existing `DataSeedContext`
Administrator properties. ABP Identity seeding remains responsible for creating
the Administrator and its `admin` role. On repeat seeding, ABP retains an
existing Administrator found by its standard username; this change does not
reset or rotate that account. The existing Reader role contributor remains
unchanged.

The migrator preserves ABP's normal host and tenant traversal. Whether a future
runtime operation may traverse tenant connection strings is a separate
Owner-approved runtime-authorization and target-ownership gate; this source
change does not grant that authority or perform a database operation.
