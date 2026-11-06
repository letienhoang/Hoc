using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Web.Menus;

public class DemoRecordsMenuContributor : IMenuContributor
{
    public Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        // A route-backed, authorized item is intentionally deferred until the module has a UI.
        return Task.CompletedTask;
    }
}
