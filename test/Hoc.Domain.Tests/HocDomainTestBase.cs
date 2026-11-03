using Volo.Abp.Modularity;

namespace Hoc;

/* Inherit from this class for your domain layer tests. */
public abstract class HocDomainTestBase<TStartupModule> : HocTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
