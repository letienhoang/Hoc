using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Hoc.EntityFrameworkCore;

/// <summary>
/// Loads the workspace-local Development connection configuration without making
/// the current working directory part of the configuration contract.
/// </summary>
public static class HocDevelopmentConfiguration
{
    private const string DevelopmentSecretsRelativePath = ".tools/config/development.secrets.json";
    private const string WorkspaceMarker = "Hoc.slnx";

    public static IConfigurationBuilder AddDevelopmentSecretsJson(
        this IConfigurationBuilder configuration,
        string environmentName,
        string? workspaceRoot = null,
        string? hostSecretsDirectory = null,
        string? currentDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return configuration;
        }

        var root = workspaceRoot ?? FindWorkspaceRoot();
        var secretsPath = Path.Combine(root, DevelopmentSecretsRelativePath);

        if (!File.Exists(secretsPath))
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' was not found under the workspace root.");
        }

        ValidateConnectionString(secretsPath);
        ValidateHostLocalConnectionString(
            hostSecretsDirectory,
            currentDirectory ?? Directory.GetCurrentDirectory());
        return configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: false);
    }

    public static IConfigurationBuilder AddHostLocalSecretsJson(
        this IConfigurationBuilder configuration,
        string? hostSecretsDirectory,
        string? currentDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var inspectedDirectories = GetHostSecretsDirectories(hostSecretsDirectory, currentDirectory);

        foreach (var directory in inspectedDirectories)
        {
            configuration.AddJsonFile(
                Path.Combine(directory, "appsettings.secrets.json"),
                optional: true,
                reloadOnChange: false);
        }

        return configuration;
    }

    public static string FindWorkspaceRoot()
    {
        foreach (var startDirectory in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, WorkspaceMarker)))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException($"Could not locate workspace root containing '{WorkspaceMarker}'.");
    }

    private static void ValidateConnectionString(string secretsPath)
    {
        IConfigurationRoot developmentConfiguration;

        NpgsqlConnectionStringBuilder connectionStringBuilder;

        try
        {
            developmentConfiguration = new ConfigurationBuilder()
                .AddJsonFile(secretsPath, optional: false, reloadOnChange: false)
                .Build();
        }
        catch (Exception exception) when (exception is FormatException or InvalidDataException or JsonException)
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' is malformed.");
        }

        var connectionString = developmentConfiguration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' must provide ConnectionStrings:Default.");
        }

        try
        {
            connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' contains an invalid PostgreSQL connection configuration.");
        }

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Host)
            || string.IsNullOrWhiteSpace(connectionStringBuilder.Database)
            || string.IsNullOrWhiteSpace(connectionStringBuilder.Username)
            || string.IsNullOrWhiteSpace(connectionStringBuilder.Password))
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' contains an incomplete PostgreSQL connection configuration.");
        }

        if (connectionStringBuilder.SslMode != SslMode.VerifyFull)
        {
            throw new InvalidOperationException(
                $"Development configuration file '{DevelopmentSecretsRelativePath}' must require PostgreSQL server identity verification.");
        }
    }

    private static void ValidateHostLocalConnectionString(
        string? hostSecretsDirectory,
        string? currentDirectory)
    {
        foreach (var directory in GetHostSecretsDirectories(hostSecretsDirectory, currentDirectory))
        {
            var hostSecretsPath = Path.Combine(directory, "appsettings.secrets.json");

            if (!File.Exists(hostSecretsPath))
            {
                continue;
            }

            try
            {
                var hostSecretsConfiguration = new ConfigurationBuilder()
                    .AddJsonFile(hostSecretsPath, optional: false, reloadOnChange: false)
                    .Build();

                if (!string.IsNullOrWhiteSpace(hostSecretsConfiguration.GetConnectionString("Default")))
                {
                    throw new InvalidOperationException(
                        "Host-local appsettings.secrets.json must not specify ConnectionStrings:Default in Development; use the shared development configuration file.");
                }
            }
            catch (Exception exception) when (exception is FormatException or InvalidDataException or JsonException)
            {
                throw new InvalidOperationException(
                    "Host-local appsettings.secrets.json is malformed.");
            }
        }
    }

    private static IReadOnlyList<string> GetHostSecretsDirectories(
        string? hostSecretsDirectory,
        string? currentDirectory)
    {
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inspectedDirectories = new List<string>();

        AddHostSecretsDirectory(hostSecretsDirectory, seenDirectories, inspectedDirectories);
        AddHostSecretsDirectory(currentDirectory ?? Directory.GetCurrentDirectory(), seenDirectories, inspectedDirectories);

        return inspectedDirectories;
    }

    private static void AddHostSecretsDirectory(
        string? directory,
        ISet<string> seenDirectories,
        ICollection<string> inspectedDirectories)
    {
        if (!string.IsNullOrWhiteSpace(directory))
        {
            var fullPath = Path.GetFullPath(directory);

            if (seenDirectories.Add(fullPath))
            {
                inspectedDirectories.Add(fullPath);
            }
        }
    }
}
