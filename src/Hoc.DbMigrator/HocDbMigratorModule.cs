using Hoc.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Hoc.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(HocEntityFrameworkCoreModule),
    typeof(HocApplicationContractsModule)
    )]
public class HocDbMigratorModule : AbpModule
{
}
