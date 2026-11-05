# Development configuration

When the environment is `Development`, Hoc.Web, Hoc.DbMigrator, and EF Core
design-time tooling load the ignored workspace file
`.tools/config/development.secrets.json`. The resolver walks upward from the
application base directory to find `Hoc.slnx`, so this does not depend on the
process current working directory.

The file must provide a syntactically valid PostgreSQL
`ConnectionStrings:Default` with a nonempty host, database, username, and
password. It must explicitly use `Ssl Mode=VerifyFull`; default, insecure, or
server-identity-unverified TLS settings are rejected. If the file is missing or
invalid in Development, startup/configuration fails before the tracked localhost
template can be used. The file is not loaded in any other environment.

In Development, configuration precedence for `ConnectionStrings:Default`, from
lowest to highest, is:

1. The host's normal default sources, including `appsettings.json`,
   environment-specific settings, User Secrets, environment variables, and
   command-line arguments. EF Core design-time tooling intentionally uses only
   the DbMigrator `appsettings.json` baseline.
2. The existing host-local `appsettings.secrets.json` overlay. The hosts load
   the content-root and current-working-directory locations (once if they are
   the same), so Development validation inspects those exact sources. It may supply
   host settings such as Administrator bootstrap configuration, but it must not
   contain `ConnectionStrings:Default`; doing so fails configuration before a
   database option is created.
3. The Development workspace file, after successful existence, JSON, and
   PostgreSQL connection-shape validation. It is added last and is therefore
   authoritative for `ConnectionStrings:Default`.

Missing, malformed, or incomplete Development shared configuration fails before
application service registration (Web), host build/run (DbMigrator), or
`DbContextOptions` creation (EF design time). In non-Development environments,
the workspace file and host-local conflict check are not selected, preserving
the existing production configuration behavior. EF Core design-time environment
selection is explicit: `DOTNET_ENVIRONMENT`, then `ASPNETCORE_ENVIRONMENT`.
When neither is set, design-time configuration fails before it can select the
tracked localhost template. This does not alter normal production host runtime
configuration.

DbMigrator's local overlay remains the place for its Administrator bootstrap
configuration. EF Core design-time tooling uses the same workspace resolution,
environment check, Development file, and DbMigrator local overlay; it remains
configuration-only and does not run migrations or connect to a database.
