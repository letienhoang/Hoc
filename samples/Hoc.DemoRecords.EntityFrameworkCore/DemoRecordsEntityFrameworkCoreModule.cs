using Hoc.DemoRecords;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Hoc.DemoRecords.EntityFrameworkCore;

[DependsOn(
    typeof(DemoRecordsDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class DemoRecordsEntityFrameworkCoreModule : AbpModule
{
}
