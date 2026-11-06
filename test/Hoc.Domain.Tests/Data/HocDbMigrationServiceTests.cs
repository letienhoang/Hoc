using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Hoc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Hoc.Data;

public class HocDbMigrationServiceTests
{
    [Fact]
    public async Task Missing_Administrator_password_should_fail_before_migration_or_seed()
    {
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.ShouldNotContain(credentials.Email);
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Empty_Administrator_email_should_fail_safely()
    {
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = string.Empty,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.Contains(credentials.Password, StringComparison.Ordinal).ShouldBeFalse();
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Empty_Administrator_password_should_fail_safely()
    {
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = string.Empty
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.Contains(credentials.Email, StringComparison.Ordinal).ShouldBeFalse();
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Invalid_Administrator_email_should_fail_safely()
    {
        const string invalidEmail = "not-an-email";
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = invalidEmail,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.Contains(invalidEmail, StringComparison.Ordinal).ShouldBeFalse();
        exception.Message.Contains(credentials.Password, StringComparison.Ordinal).ShouldBeFalse();
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Invalid_Administrator_password_should_fail_safely()
    {
        const string invalidPassword = "short";
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = invalidPassword
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.Contains(credentials.Email, StringComparison.Ordinal).ShouldBeFalse();
        exception.Message.Contains(invalidPassword, StringComparison.Ordinal).ShouldBeFalse();
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Template_default_Administrator_password_should_be_rejected_without_exposing_it()
    {
        var credentials = CreateValidCredentials();
        var templatePassword = IdentityDataSeedContributor.AdminPasswordDefaultValue;
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = templatePassword
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        exception.Message.ShouldNotContain(templatePassword);
        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Valid_configured_credentials_should_reach_the_existing_DataSeedContext_contract()
    {
        var credentials = CreateValidCredentials();
        DataSeedContext? receivedContext = null;
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password
        });
        fixture.DataSeeder.SeedAsync(Arg.Any<DataSeedContext>()).Returns(callInfo =>
        {
            receivedContext = callInfo.Arg<DataSeedContext>();
            return Task.CompletedTask;
        });

        await fixture.Service.MigrateAsync();

        receivedContext.ShouldNotBeNull();
        receivedContext![IdentityDataSeedContributor.AdminEmailPropertyName].ShouldBe(credentials.Email);
        receivedContext[IdentityDataSeedContributor.AdminPasswordPropertyName].ShouldBe(credentials.Password);
        await fixture.Migrator.Received(1).MigrateAsync();
        await fixture.DataSeeder.Received(1).SeedAsync(Arg.Any<DataSeedContext>());
    }

    [Fact]
    public async Task Development_host_only_mode_should_migrate_and_seed_only_the_host()
    {
        var credentials = CreateValidCredentials();
        var tenant = CreateTenant("tenant-for-host-only-test", null);
        var receivedContexts = new List<DataSeedContext>();
        var fixture = CreateFixture(
            new Dictionary<string, string?>
            {
                [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
                [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password,
                [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = "true"
            },
            Environments.Development,
            new List<Tenant> { tenant });
        fixture.DataSeeder.SeedAsync(Arg.Any<DataSeedContext>()).Returns(callInfo =>
        {
            receivedContexts.Add(callInfo.Arg<DataSeedContext>());
            return Task.CompletedTask;
        });

        await fixture.Service.MigrateAsync();

        await fixture.Migrator.Received(1).MigrateAsync();
        await fixture.DataSeeder.Received(1).SeedAsync(Arg.Any<DataSeedContext>());
        receivedContexts.Count.ShouldBe(1);
        receivedContexts[0].TenantId.ShouldBeNull();
        await fixture.TenantRepository.DidNotReceive().GetListAsync(includeDetails: true);
        fixture.CurrentTenant.DidNotReceive().Change(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task Default_mode_should_preserve_tenant_migration_and_seeding()
    {
        var credentials = CreateValidCredentials();
        var tenant = CreateTenant(
            "tenant-for-default-mode-test",
            "Host=tenant.example;Database=synthetic;Username=synthetic");
        tenant.SetConnectionString("Default", "Host=tenant.example;Database=synthetic;Username=synthetic");
        var receivedContexts = new List<DataSeedContext>();
        var fixture = CreateFixture(
            new Dictionary<string, string?>
            {
                [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
                [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password,
                [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = "false"
            },
            Environments.Production,
            new List<Tenant> { tenant });
        fixture.DataSeeder.SeedAsync(Arg.Any<DataSeedContext>()).Returns(callInfo =>
        {
            receivedContexts.Add(callInfo.Arg<DataSeedContext>());
            return Task.CompletedTask;
        });

        await fixture.Service.MigrateAsync();

        await fixture.Migrator.Received(2).MigrateAsync();
        await fixture.DataSeeder.Received(2).SeedAsync(Arg.Any<DataSeedContext>());
        receivedContexts.ShouldContain(context => context.TenantId == null);
        receivedContexts.ShouldContain(context => context.TenantId == tenant.Id);
        await fixture.TenantRepository.Received(1).GetListAsync(includeDetails: true);
        fixture.CurrentTenant.Received(1).Change(tenant.Id);
    }

    [Theory]
    [InlineData("Production", "true")]
    [InlineData("Development", "")]
    [InlineData("Development", " ")]
    [InlineData("Development", "not-a-boolean")]
    public async Task Unsupported_host_only_configuration_should_fail_before_writes(
        string environmentName,
        string hostOnlyValue)
    {
        var credentials = CreateValidCredentials();
        var fixture = CreateFixture(new Dictionary<string, string?>
        {
            [HocDbMigrationService.BootstrapAdministratorEmailConfigurationKey] = credentials.Email,
            [HocDbMigrationService.BootstrapAdministratorPasswordConfigurationKey] = credentials.Password,
            [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = hostOnlyValue
        }, environmentName);

        await Should.ThrowAsync<InvalidOperationException>(fixture.Service.MigrateAsync());

        await fixture.Migrator.DidNotReceive().MigrateAsync();
        await fixture.DataSeeder.DidNotReceive().SeedAsync(Arg.Any<DataSeedContext>());
        await fixture.TenantRepository.DidNotReceive().GetListAsync(includeDetails: true);
    }

    [Theory]
    [InlineData("Production", "true")]
    [InlineData("Development", "")]
    [InlineData("Development", " ")]
    [InlineData("Development", "not-a-boolean")]
    public void Host_only_mode_validation_should_fail_before_ABP_initialization(
        string environmentName,
        string hostOnlyValue)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = hostOnlyValue
            })
            .Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);

        Should.Throw<InvalidOperationException>(() =>
            HocDbMigrationService.IsHostOnlyMigrationEnabled(configuration, hostEnvironment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void Missing_or_false_host_only_mode_should_preserve_default_behavior(string? hostOnlyValue)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = hostOnlyValue
            })
            .Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(Environments.Production);

        HocDbMigrationService.IsHostOnlyMigrationEnabled(configuration, hostEnvironment).ShouldBeFalse();
    }

    [Fact]
    public void Development_true_host_only_mode_should_be_enabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HocDbMigrationService.HostOnlyMigrationConfigurationKey] = "true"
            })
            .Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(Environments.Development);

        HocDbMigrationService.IsHostOnlyMigrationEnabled(configuration, hostEnvironment).ShouldBeTrue();
    }

    private static MigrationServiceFixture CreateFixture(
        IDictionary<string, string?> configurationValues,
        string environmentName = "Production",
        List<Tenant>? tenants = null)
    {
        var dataSeeder = Substitute.For<IDataSeeder>();
        dataSeeder.SeedAsync(Arg.Any<DataSeedContext>()).Returns(Task.CompletedTask);

        var migrator = Substitute.For<IHocDbSchemaMigrator>();
        migrator.MigrateAsync().Returns(Task.CompletedTask);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetListAsync(includeDetails: true).Returns(Task.FromResult(tenants ?? new List<Tenant>()));

        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var service = new HocDbMigrationService(
            configuration,
            dataSeeder,
            new[] { migrator },
            hostEnvironment,
            Options.Create(new IdentityOptions()),
            tenantRepository,
            currentTenant);

        return new MigrationServiceFixture(service, dataSeeder, migrator, tenantRepository, currentTenant);
    }

    private static BootstrapAdministratorCredentialsForTest CreateValidCredentials()
    {
        var nonce = Guid.NewGuid().ToString("N");
        return new BootstrapAdministratorCredentialsForTest(
            $"bootstrap-{nonce}@hoc.test",
            $"Bootstrap-{nonce}aA1!");
    }

    private static Tenant CreateTenant(string name, string? connectionString)
    {
        var constructor = typeof(Tenant).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(Guid), typeof(string), typeof(string) },
            modifiers: null);

        constructor.ShouldNotBeNull();

        return (Tenant)constructor.Invoke(new object?[] { Guid.NewGuid(), name, connectionString });
    }

    private sealed record MigrationServiceFixture(
        HocDbMigrationService Service,
        IDataSeeder DataSeeder,
        IHocDbSchemaMigrator Migrator,
        ITenantRepository TenantRepository,
        ICurrentTenant CurrentTenant);

    private sealed record BootstrapAdministratorCredentialsForTest(string Email, string Password);
}
