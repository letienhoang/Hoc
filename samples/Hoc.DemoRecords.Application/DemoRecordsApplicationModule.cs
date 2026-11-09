using Volo.Abp.Modularity;

namespace Hoc.DemoRecords;

[DependsOn(
    typeof(DemoRecordsDomainModule),
    typeof(DemoRecordsApplicationContractsModule)
)]
public class DemoRecordsApplicationModule : AbpModule
{
}
