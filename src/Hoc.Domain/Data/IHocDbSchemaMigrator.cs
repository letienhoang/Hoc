using System.Threading.Tasks;

namespace Hoc.Data;

public interface IHocDbSchemaMigrator
{
    Task MigrateAsync();
}
