using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Hoc.Data;

public class HocDbMigrationService : ITransientDependency
{
    public const string BootstrapAdministratorEmailConfigurationKey = "Bootstrap:Administrator:Email";
    public const string BootstrapAdministratorPasswordConfigurationKey = "Bootstrap:Administrator:Password";
    public const string HostOnlyMigrationConfigurationKey = "Migration:HostOnly";

    public ILogger<HocDbMigrationService> Logger { get; set; }

    private readonly IConfiguration _configuration;
    private readonly IDataSeeder _dataSeeder;
    private readonly IEnumerable<IHocDbSchemaMigrator> _dbSchemaMigrators;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;

    public HocDbMigrationService(
        IConfiguration configuration,
        IDataSeeder dataSeeder,
        IEnumerable<IHocDbSchemaMigrator> dbSchemaMigrators,
        IHostEnvironment hostEnvironment,
        IOptions<IdentityOptions> identityOptions,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant)
    {
        _configuration = configuration;
        _dataSeeder = dataSeeder;
        _dbSchemaMigrators = dbSchemaMigrators;
        _hostEnvironment = hostEnvironment;
        _identityOptions = identityOptions;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;

        Logger = NullLogger<HocDbMigrationService>.Instance;
    }

    public async Task MigrateAsync()
    {
        var hostOnlyMigrationEnabled = IsHostOnlyMigrationEnabled(_configuration, _hostEnvironment);
        var bootstrapAdministratorCredentials = GetBootstrapAdministratorCredentials();

        if (hostOnlyMigrationEnabled)
        {
            await MigrateDatabaseSchemaAsync();
            await SeedDataAsync(bootstrapAdministratorCredentials);

            Logger.LogInformation("Successfully completed host-only database migration.");
            return;
        }

        var initialMigrationAdded = AddInitialMigrationIfNotExist();

        if (initialMigrationAdded)
        {
            return;
        }

        Logger.LogInformation("Started database migrations...");

        await MigrateDatabaseSchemaAsync();
        await SeedDataAsync(bootstrapAdministratorCredentials);

        Logger.LogInformation($"Successfully completed host database migrations.");

        var tenants = await _tenantRepository.GetListAsync(includeDetails: true);

        var migratedDatabaseSchemas = new HashSet<string>();
        foreach (var tenant in tenants)
        {
            using (_currentTenant.Change(tenant.Id))
            {
                if (tenant.ConnectionStrings.Any())
                {
                    var tenantConnectionStrings = tenant.ConnectionStrings
                        .Select(x => x.Value)
                        .ToList();

                    if (!migratedDatabaseSchemas.IsSupersetOf(tenantConnectionStrings))
                    {
                        await MigrateDatabaseSchemaAsync(tenant);

                        migratedDatabaseSchemas.AddIfNotContains(tenantConnectionStrings);
                    }
                }

                await SeedDataAsync(bootstrapAdministratorCredentials, tenant);
            }

            Logger.LogInformation($"Successfully completed {tenant.Name} tenant database migrations.");
        }

        Logger.LogInformation("Successfully completed all database migrations.");
        Logger.LogInformation("You can safely end this process...");
    }

    private async Task MigrateDatabaseSchemaAsync(Tenant? tenant = null)
    {
        Logger.LogInformation(
            $"Migrating schema for {(tenant == null ? "host" : tenant.Name + " tenant")} database...");

        foreach (var migrator in _dbSchemaMigrators)
        {
            await migrator.MigrateAsync();
        }
    }

    private async Task SeedDataAsync(
        BootstrapAdministratorCredentials bootstrapAdministratorCredentials,
        Tenant? tenant = null)
    {
        Logger.LogInformation($"Executing {(tenant == null ? "host" : tenant.Name + " tenant")} database seed...");

        await _dataSeeder.SeedAsync(new DataSeedContext(tenant?.Id)
            .WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, bootstrapAdministratorCredentials.Email)
            .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, bootstrapAdministratorCredentials.Password)
        );
    }

    private BootstrapAdministratorCredentials GetBootstrapAdministratorCredentials()
    {
        var email = _configuration[BootstrapAdministratorEmailConfigurationKey];
        var password = _configuration[BootstrapAdministratorPasswordConfigurationKey];

        if (!IsValidBootstrapAdministratorEmail(email) ||
            !IsValidBootstrapAdministratorPassword(password))
        {
            throw new InvalidOperationException(
                "Bootstrap Administrator credentials configuration is missing or invalid.");
        }

        return new BootstrapAdministratorCredentials(email!, password!);
    }

    public static bool IsHostOnlyMigrationEnabled(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var hostOnlyValue = configuration[HostOnlyMigrationConfigurationKey];

        if (string.IsNullOrWhiteSpace(hostOnlyValue))
        {
            return false;
        }

        if (!bool.TryParse(hostOnlyValue, out var hostOnly))
        {
            throw new InvalidOperationException("Migration HostOnly configuration is invalid.");
        }

        if (!hostOnly)
        {
            return false;
        }

        if (!hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("Host-only migration mode is supported only in Development.");
        }

        return true;
    }

    private bool IsValidBootstrapAdministratorPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.Equals(
                password,
                IdentityDataSeedContributor.AdminPasswordDefaultValue,
                StringComparison.Ordinal))
        {
            return false;
        }

        var options = _identityOptions.Value.Password;

        return password.Length >= options.RequiredLength &&
            (!options.RequireNonAlphanumeric || password.Any(character => !char.IsLetterOrDigit(character))) &&
            (!options.RequireLowercase || password.Any(char.IsLower)) &&
            (!options.RequireUppercase || password.Any(char.IsUpper)) &&
            (!options.RequireDigit || password.Any(char.IsDigit)) &&
            password.Distinct().Count() >= options.RequiredUniqueChars;
    }

    private static bool IsValidBootstrapAdministratorEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var parsedEmail = new MailAddress(email);
            return string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record BootstrapAdministratorCredentials(string Email, string Password);

    private bool AddInitialMigrationIfNotExist()
    {
        try
        {
            if (!DbMigrationsProjectExists())
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        try
        {
            if (!MigrationsFolderExists())
            {
                AddInitialMigration();
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning("Couldn't determinate if any migrations exist : " + e.Message);
            return false;
        }
    }

    private bool DbMigrationsProjectExists()
    {
        var dbMigrationsProjectFolder = GetEntityFrameworkCoreProjectFolderPath();

        return dbMigrationsProjectFolder != null;
    }

    private bool MigrationsFolderExists()
    {
        var dbMigrationsProjectFolder = GetEntityFrameworkCoreProjectFolderPath();
        return dbMigrationsProjectFolder != null && Directory.Exists(Path.Combine(dbMigrationsProjectFolder, "Migrations"));
    }

    private void AddInitialMigration()
    {
        Logger.LogInformation("Creating initial migration...");

        string argumentPrefix;
        string fileName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            argumentPrefix = "-c";
            fileName = "/bin/bash";
        }
        else
        {
            argumentPrefix = "/C";
            fileName = "cmd.exe";
        }

        var procStartInfo = new ProcessStartInfo(fileName,
            $"{argumentPrefix} \"abp create-migration-and-run-migrator \"{GetEntityFrameworkCoreProjectFolderPath()}\"\""
        );

        try
        {
            Process.Start(procStartInfo);
        }
        catch (Exception)
        {
            throw new Exception("Couldn't run ABP CLI...");
        }
    }

    private string? GetEntityFrameworkCoreProjectFolderPath()
    {
        var slnDirectoryPath = GetSolutionDirectoryPath();

        if (slnDirectoryPath == null)
        {
            throw new Exception("Solution folder not found!");
        }

        var srcDirectoryPath = Path.Combine(slnDirectoryPath, "src");

        return Directory.GetDirectories(srcDirectoryPath)
            .FirstOrDefault(d => d.EndsWith(".EntityFrameworkCore"));
    }

    private string? GetSolutionDirectoryPath()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory != null && Directory.GetParent(currentDirectory.FullName) != null)
        {
            currentDirectory = Directory.GetParent(currentDirectory.FullName);

            if (currentDirectory != null && Directory.GetFiles(currentDirectory.FullName).FirstOrDefault(f => f.EndsWith(".sln")) != null)
            {
                return currentDirectory.FullName;
            }
        }

        return null;
    }
}
