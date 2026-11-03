using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Hoc.Data;

/* This is used if database provider does't define
 * IHocDbSchemaMigrator implementation.
 */
public class NullHocDbSchemaMigrator : IHocDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
