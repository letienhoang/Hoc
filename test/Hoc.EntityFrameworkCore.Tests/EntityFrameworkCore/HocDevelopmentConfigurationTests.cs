using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hoc.EntityFrameworkCore;

public class HocDevelopmentConfigurationTests : IDisposable
{
    private readonly string _workspaceRoot = Path.Combine(Path.GetTempPath(), $"hoc-config-{Guid.NewGuid():N}");

    public HocDevelopmentConfigurationTests()
    {
        Directory.CreateDirectory(_workspaceRoot);
        File.WriteAllText(Path.Combine(_workspaceRoot, "Hoc.slnx"), "<Solution />");
    }

    [Fact]
    public void Development_shared_file_overrides_the_tracked_template_after_host_local_settings()
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);
        WriteHostSecrets("{ \"Bootstrap\": { \"AdministratorUserName\": \"synthetic-admin\" } }");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:Default", "Host=localhost;Port=5432;Database=Hoc;Username=hoc")
            })
            .AddJsonFile(Path.Combine(_workspaceRoot, "host", "appsettings.secrets.json"), optional: false)
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host"))
            .Build();

        Assert.StartsWith("Host=neon.example", configuration.GetConnectionString("Default"), StringComparison.Ordinal);
        Assert.Equal("synthetic-admin", configuration["Bootstrap:AdministratorUserName"]);
    }

    [Fact]
    public void Development_shared_file_is_loaded_when_no_host_secret_overlay_exists()
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:Default", "Host=localhost;Port=5432;Database=Hoc;Username=hoc")
            })
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host"))
            .Build();

        Assert.StartsWith("Host=neon.example", configuration.GetConnectionString("Default"), StringComparison.Ordinal);
    }

    [Fact]
    public void Development_missing_shared_file_fails_before_template_can_be_used()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddInMemoryCollection()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("was not found", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_invalid_shared_connection_fails_before_template_can_be_used()
    {
        WriteDevelopmentSecrets("Host=;Database=hoc;Username=neon_user;Password=synthetic;Ssl Mode=VerifyFull");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddInMemoryCollection()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("incomplete", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Non_development_environment_does_not_select_the_workspace_file()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:Default", "Host=localhost;Database=hoc;Username=hoc")
            })
            .AddDevelopmentSecretsJson(Environments.Production, _workspaceRoot, Path.Combine(_workspaceRoot, "host"))
            .Build();

        Assert.StartsWith("Host=localhost", configuration.GetConnectionString("Default"), StringComparison.Ordinal);
    }

    [Fact]
    public void Design_time_factory_uses_the_same_development_file()
    {
        var migratorDirectory = Path.Combine(_workspaceRoot, "src", "Hoc.DbMigrator");
        Directory.CreateDirectory(migratorDirectory);
        File.WriteAllText(
            Path.Combine(migratorDirectory, "appsettings.json"),
            "{ \"ConnectionStrings\": { \"Default\": \"Host=localhost;Database=Hoc;Username=hoc\" } }");
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);

        var configuration = HocDbContextFactory.BuildConfiguration(_workspaceRoot, Environments.Development);

        Assert.StartsWith("Host=neon.example", configuration.GetConnectionString("Default"), StringComparison.Ordinal);
    }

    [Fact]
    public void Design_time_factory_rejects_a_host_local_connection_conflict()
    {
        var migratorDirectory = Path.Combine(_workspaceRoot, "src", "Hoc.DbMigrator");
        Directory.CreateDirectory(migratorDirectory);
        File.WriteAllText(
            Path.Combine(migratorDirectory, "appsettings.json"),
            "{ \"ConnectionStrings\": { \"Default\": \"Host=localhost;Database=Hoc;Username=hoc\" } }");
        File.WriteAllText(
            Path.Combine(migratorDirectory, "appsettings.secrets.json"),
            "{ \"ConnectionStrings\": { \"Default\": \"Host=host-overlay.example;Database=hoc;Username=overlay_user;Password=synthetic;Ssl Mode=VerifyFull\" } }");
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            HocDbContextFactory.BuildConfiguration(_workspaceRoot, Environments.Development));

        Assert.Contains("must not specify", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Development", "Production", "Development")]
    [InlineData(null, "Development", "Development")]
    public void Design_time_environment_selection_uses_an_explicit_environment(
        string? dotnetEnvironment,
        string? aspnetcoreEnvironment,
        string expectedEnvironment)
    {
        Assert.Equal(
            expectedEnvironment,
            HocDbContextFactory.ResolveEnvironmentName(dotnetEnvironment, aspnetcoreEnvironment));
    }

    [Fact]
    public void Design_time_environment_selection_fails_closed_when_no_environment_is_set()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            HocDbContextFactory.ResolveEnvironmentName(null, null));

        Assert.Contains("requires", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_host_local_connection_conflict_fails_closed()
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);
        WriteHostSecrets("{ \"ConnectionStrings\": { \"Default\": \"Host=host-overlay.example;Database=hoc;Username=overlay_user;Password=synthetic;Ssl Mode=VerifyFull\" } }");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(_workspaceRoot, "host", "appsettings.secrets.json"), optional: false)
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("must not specify", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_malformed_shared_file_fails_closed()
    {
        var configurationDirectory = Path.Combine(_workspaceRoot, ".tools", "config");
        Directory.CreateDirectory(configurationDirectory);
        File.WriteAllText(Path.Combine(configurationDirectory, "development.secrets.json"), "{ malformed");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("malformed", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("Host=neon.example;Database=hoc;Username=neon_user;Ssl Mode=VerifyFull")]
    [InlineData("Host=neon.example;Database=hoc;Username=neon_user;Password=synthetic;Ssl Mode=Disable")]
    [InlineData("Host=neon.example;Database=hoc;Username=neon_user;Password=synthetic")]
    public void Development_connection_requires_password_and_verified_tls(string connectionString)
    {
        WriteDevelopmentSecrets(connectionString);

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.DoesNotContain("synthetic", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Development_invalid_connection_does_not_expose_secret_derived_parser_details()
    {
        WriteDevelopmentSecrets("Host=neon.example;Database=hoc;Username=neon_user;Password=secret-derived-value;Ssl Mode=VerifyFull;unknown-setting=value");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("invalid PostgreSQL", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-derived-value", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Development_malformed_host_local_file_does_not_retain_parser_exception()
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);
        WriteHostSecrets("{ malformed");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, Path.Combine(_workspaceRoot, "host")));

        Assert.Contains("Host-local", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("repository")]
    [InlineData("src/Hoc.Web")]
    [InlineData("src/Hoc.DbMigrator")]
    public void Development_configuration_rejects_a_conflict_from_each_launch_directory(string launchDirectoryRelativePath)
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);
        var launchDirectory = Path.Combine(_workspaceRoot, launchDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(launchDirectory);
        File.WriteAllText(
            Path.Combine(launchDirectory, "appsettings.secrets.json"),
            "{ \"ConnectionStrings\": { \"Default\": \"Host=conflict.example;Database=hoc;Username=conflict;Password=synthetic;Ssl Mode=VerifyFull\" } }");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddDevelopmentSecretsJson(Environments.Development, _workspaceRoot, launchDirectory, launchDirectory));

        Assert.Contains("must not specify", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("repository")]
    [InlineData("src/Hoc.Web")]
    [InlineData("src/Hoc.DbMigrator")]
    public void Effective_host_configuration_keeps_shared_connection_authoritative_for_each_launch_directory(string launchDirectoryRelativePath)
    {
        WriteDevelopmentSecrets(ValidDevelopmentConnectionString);
        var launchDirectory = Path.Combine(_workspaceRoot, launchDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(launchDirectory);
        File.WriteAllText(
            Path.Combine(launchDirectory, "appsettings.secrets.json"),
            "{ \"Bootstrap\": { \"AdministratorUserName\": \"synthetic-admin\" } }");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(launchDirectory)
            .AddHostLocalSecretsJson(launchDirectory, launchDirectory)
            .AddDevelopmentSecretsJson(
                Environments.Development,
                _workspaceRoot,
                launchDirectory,
                launchDirectory)
            .Build();

        Assert.StartsWith("Host=neon.example", configuration.GetConnectionString("Default"), StringComparison.Ordinal);
        Assert.Equal("synthetic-admin", configuration["Bootstrap:AdministratorUserName"]);
    }

    public void Dispose()
    {
        Directory.Delete(_workspaceRoot, recursive: true);
    }

    private void WriteDevelopmentSecrets(string connectionString)
    {
        var configurationDirectory = Path.Combine(_workspaceRoot, ".tools", "config");
        Directory.CreateDirectory(configurationDirectory);
        File.WriteAllText(
            Path.Combine(configurationDirectory, "development.secrets.json"),
            $$"""
            {
              "ConnectionStrings": {
                "Default": "{{connectionString}}"
              }
            }
            """);
    }

    private void WriteHostSecrets(string json)
    {
        var hostDirectory = Path.Combine(_workspaceRoot, "host");
        Directory.CreateDirectory(hostDirectory);
        File.WriteAllText(Path.Combine(hostDirectory, "appsettings.secrets.json"), json);
    }

    private const string ValidDevelopmentConnectionString =
        "Host=neon.example;Database=hoc;Username=neon_user;Password=synthetic;Ssl Mode=VerifyFull";
}
