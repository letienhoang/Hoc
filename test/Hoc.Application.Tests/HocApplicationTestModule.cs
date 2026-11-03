using Volo.Abp.Modularity;

namespace Hoc;

[DependsOn(
    typeof(HocApplicationModule),
    typeof(HocDomainTestModule)
)]
public class HocApplicationTestModule : AbpModule
{

}
