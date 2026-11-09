using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace Hoc.DemoRecords.HttpApi;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(DemoRecordsApplicationModule)
)]
public class DemoRecordsHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(
                typeof(DemoRecordsApplicationModule).Assembly,
                configuration => configuration.RootPath = "demo-records");
        });
    }
}
