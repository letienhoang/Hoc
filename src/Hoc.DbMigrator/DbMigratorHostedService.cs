using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hoc.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace Hoc.DbMigrator;

public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public DbMigratorHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HocDbMigrationService.IsHostOnlyMigrationEnabled(_configuration, _hostEnvironment);

        using (var application = await AbpApplicationFactory.CreateAsync<HocDbMigratorModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.Services.AddSingleton(_hostEnvironment);
           options.UseAutofac();
           options.Services.AddLogging(c => c.AddSerilog());
           options.AddDataMigrationEnvironment();
        }))
        {
            await application.InitializeAsync();

            await application
                .ServiceProvider
                .GetRequiredService<HocDbMigrationService>()
                .MigrateAsync();

            await application.ShutdownAsync();

            _hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
