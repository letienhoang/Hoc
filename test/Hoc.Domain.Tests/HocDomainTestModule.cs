using Volo.Abp.Modularity;

namespace Hoc;

[DependsOn(
    typeof(HocDomainModule),
    typeof(HocTestBaseModule)
)]
public class HocDomainTestModule : AbpModule
{

}
