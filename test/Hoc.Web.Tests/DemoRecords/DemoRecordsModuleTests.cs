using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hoc.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Hoc.DemoRecords.Web;
using Hoc.DemoRecords.Web.Menus;
using Hoc.Web;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Xunit;

namespace Hoc.DemoRecords.Tests;

public class DemoRecordsModuleTests : HocWebTestBase
{
    [Fact]
    public async Task Demo_records_permissions_should_be_available_with_stable_names()
    {
        var permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();

        (await permissionDefinitionManager.GetAsync(DemoRecordsPermissions.Read)).Name.ShouldBe(DemoRecordsPermissions.Read);
        (await permissionDefinitionManager.GetAsync(DemoRecordsPermissions.Create)).Name.ShouldBe(DemoRecordsPermissions.Create);
        (await permissionDefinitionManager.GetAsync(DemoRecordsPermissions.Update)).Name.ShouldBe(DemoRecordsPermissions.Update);
        (await permissionDefinitionManager.GetAsync(DemoRecordsPermissions.Delete)).Name.ShouldBe(DemoRecordsPermissions.Delete);
    }

    [Fact]
    public void Web_host_should_depend_on_the_demo_records_web_module()
    {
        var dependsOn = typeof(HocWebModule).GetCustomAttribute<DependsOnAttribute>();

        dependsOn.ShouldNotBeNull();
        dependsOn.DependedTypes.ShouldContain(typeof(DemoRecordsWebModule));
    }

    [Fact]
    public void Demo_records_menu_contributor_should_be_registered()
    {
        var navigationOptions = GetRequiredService<IOptions<AbpNavigationOptions>>().Value;

        navigationOptions.MenuContributors.ShouldContain(contributor => contributor is DemoRecordsMenuContributor);
    }

    [Fact]
    public async Task Demo_records_should_not_expose_a_menu_item_before_an_authorized_route_exists()
    {
        var menuManager = GetRequiredService<IMenuManager>();
        var mainMenu = await menuManager.GetMainMenuAsync();

        mainMenu.FindMenuItem(DemoRecordsMenus.DemoRecords).ShouldBeNull();
        mainMenu.Items.Any(item => item.Url?.Contains("DemoRecords") == true).ShouldBeFalse();
    }
}
