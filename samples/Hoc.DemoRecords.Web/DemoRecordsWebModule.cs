using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Web;

[DependsOn(
    typeof(DemoRecordsApplicationContractsModule),
    typeof(AbpUiNavigationModule)
)]
public class DemoRecordsWebModule : AbpModule
{
}
