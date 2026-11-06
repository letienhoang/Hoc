using Hoc.DemoRecords.Localization;
using Volo.Abp.Authorization;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Hoc.DemoRecords;

[DependsOn(typeof(AbpAuthorizationModule))]
public class DemoRecordsApplicationContractsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<DemoRecordsApplicationContractsModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<DemoRecordsResource>("en")
                .AddVirtualJson("/Localization/DemoRecords");
        });
    }
}
