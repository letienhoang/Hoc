using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hoc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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

    private static MigrationServiceFixture CreateFixture(IDictionary<string, string?> configurationValues)
    {
        var dataSeeder = Substitute.For<IDataSeeder>();
        dataSeeder.SeedAsync(Arg.Any<DataSeedContext>()).Returns(Task.CompletedTask);

        var migrator = Substitute.For<IHocDbSchemaMigrator>();
        migrator.MigrateAsync().Returns(Task.CompletedTask);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetListAsync(includeDetails: true).Returns(Task.FromResult(new List<Tenant>()));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var service = new HocDbMigrationService(
            configuration,
            dataSeeder,
            new[] { migrator },
            Options.Create(new IdentityOptions()),
            tenantRepository,
            Substitute.For<ICurrentTenant>());

        return new MigrationServiceFixture(service, dataSeeder, migrator);
    }

    private static BootstrapAdministratorCredentialsForTest CreateValidCredentials()
    {
        var nonce = Guid.NewGuid().ToString("N");
        return new BootstrapAdministratorCredentialsForTest(
            $"bootstrap-{nonce}@hoc.test",
            $"Bootstrap-{nonce}aA1!");
    }

    private sealed record MigrationServiceFixture(
        HocDbMigrationService Service,
        IDataSeeder DataSeeder,
        IHocDbSchemaMigrator Migrator);

    private sealed record BootstrapAdministratorCredentialsForTest(string Email, string Password);
}
