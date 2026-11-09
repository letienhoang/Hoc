using Hoc.DemoRecords.Permissions;
using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Web.Menus;

public class DemoRecordsMenuContributor : IMenuContributor
{
    public Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        var localization = context.GetLocalizer<Localization.DemoRecordsResource>();
        var item = new ApplicationMenuItem(
            DemoRecordsMenus.DemoRecords,
            localization["Menu:DemoRecords"],
            url: "/DemoRecords",
            icon: "fas fa-list",
            requiredPermissionName: DemoRecordsPermissions.Read);

        context.Menu.AddItem(item);
        return Task.CompletedTask;
    }
}
