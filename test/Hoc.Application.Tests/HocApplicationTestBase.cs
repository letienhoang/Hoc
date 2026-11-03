using Volo.Abp.Modularity;

namespace Hoc;

public abstract class HocApplicationTestBase<TStartupModule> : HocTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
