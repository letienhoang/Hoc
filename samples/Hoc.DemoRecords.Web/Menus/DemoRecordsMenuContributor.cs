using Hoc.DemoRecords.Permissions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Hoc.DemoRecords.Web.Menus;

public class DemoRecordsMenuContributor : IMenuContributor
{
    public Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.Main)
        {
            return Task.CompletedTask;
        }

        var currentTenant = context.ServiceProvider.GetRequiredService<ICurrentTenant>();
        var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
        if (currentTenant.Id.HasValue || currentUser.TenantId.HasValue)
        {
            return Task.CompletedTask;
        }

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
