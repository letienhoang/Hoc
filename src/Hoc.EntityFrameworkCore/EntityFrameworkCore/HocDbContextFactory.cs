using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hoc.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class HocDbContextFactory : IDesignTimeDbContextFactory<HocDbContext>
{
    public HocDbContext CreateDbContext(string[] args)
    {
        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        HocEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<HocDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Default"));

        return new HocDbContext(builder.Options);
    }

    internal static IConfigurationRoot BuildConfiguration(string? workspaceRoot = null, string? environmentName = null)
    {
        var root = workspaceRoot ?? HocDevelopmentConfiguration.FindWorkspaceRoot();
        var migratorDirectory = Path.Combine(root, "src", "Hoc.DbMigrator");
        var resolvedEnvironmentName = environmentName
            ?? ResolveEnvironmentName(
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

        var builder = new ConfigurationBuilder()
            .SetBasePath(migratorDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.secrets.json", optional: true)
            .AddDevelopmentSecretsJson(
                resolvedEnvironmentName,
                root,
                migratorDirectory);

        return builder.Build();
    }

    internal static string ResolveEnvironmentName(string? dotnetEnvironment, string? aspnetcoreEnvironment) =>
        !string.IsNullOrWhiteSpace(dotnetEnvironment)
            ? dotnetEnvironment
            : !string.IsNullOrWhiteSpace(aspnetcoreEnvironment)
                ? aspnetcoreEnvironment
                : throw new InvalidOperationException(
                    "EF Core design-time configuration requires DOTNET_ENVIRONMENT or ASPNETCORE_ENVIRONMENT to be set explicitly.");
}
