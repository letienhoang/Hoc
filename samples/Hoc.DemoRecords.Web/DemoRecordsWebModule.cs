using Hoc.DemoRecords.Web.Menus;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Web;

[DependsOn(
    typeof(DemoRecordsApplicationContractsModule),
    typeof(AbpUiNavigationModule)
)]
public class DemoRecordsWebModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new DemoRecordsMenuContributor());
        });
    }
}
