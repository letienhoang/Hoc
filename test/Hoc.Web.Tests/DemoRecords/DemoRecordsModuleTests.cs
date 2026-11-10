using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hoc.DemoRecords;
using Hoc.DemoRecords.Localization;
using Hoc.DemoRecords.Permissions;
using Hoc.DemoRecords.Web;
using Hoc.DemoRecords.Web.Menus;
using Hoc.Web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Xunit;

namespace Hoc.DemoRecords.Tests;

public class DemoRecordsModuleTests : HocWebTestBase
{
    [Fact]
    public void Demo_records_localization_should_resolve_every_key_in_English_and_Vietnamese()
    {
        var localizer = GetRequiredService<IStringLocalizer<DemoRecordsResource>>();
        var localizationOptions = GetRequiredService<IOptions<AbpLocalizationOptions>>().Value;
        var expectedTexts = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new()
            {
                ["Permission:DemoRecords"] = "Demo records",
                ["Permission:DemoRecords.Read"] = "Read demo records",
                ["Permission:DemoRecords.Create"] = "Create demo records",
                ["Permission:DemoRecords.Update"] = "Update demo records",
                ["Permission:DemoRecords.Delete"] = "Delete demo records",
                ["Menu:DemoRecords"] = "Demo records",
                ["DemoRecords:Title"] = "Demo records",
                ["DemoRecords:Details"] = "Demo record details",
                ["DemoRecords:Create"] = "Create demo record",
                ["DemoRecords:Edit"] = "Edit demo record",
                ["DemoRecords:Delete"] = "Delete demo record",
                ["DemoRecords:Name"] = "Name",
                ["DemoRecords:Description"] = "Description",
                ["DemoRecords:Actions"] = "Actions",
                ["DemoRecords:Save"] = "Save",
                ["DemoRecords:Cancel"] = "Cancel",
                ["DemoRecords:BackToList"] = "Back to list",
                ["DemoRecords:NoRecords"] = "No demo records are available.",
                ["DemoRecords:DeleteConfirmation"] = "Are you sure you want to delete this demo record?",
                ["DemoRecords:Previous"] = "Previous",
                ["DemoRecords:Next"] = "Next",
                ["DemoRecords:OperationFailed"] = "The request could not be completed. Please correct the highlighted values and try again."
            },
            ["vi"] = new()
            {
                ["Permission:DemoRecords"] = "Bản ghi minh họa",
                ["Permission:DemoRecords.Read"] = "Xem bản ghi minh họa",
                ["Permission:DemoRecords.Create"] = "Tạo bản ghi minh họa",
                ["Permission:DemoRecords.Update"] = "Cập nhật bản ghi minh họa",
                ["Permission:DemoRecords.Delete"] = "Xóa bản ghi minh họa",
                ["Menu:DemoRecords"] = "Bản ghi minh họa",
                ["DemoRecords:Title"] = "Bản ghi minh họa",
                ["DemoRecords:Details"] = "Chi tiết bản ghi minh họa",
                ["DemoRecords:Create"] = "Tạo bản ghi minh họa",
                ["DemoRecords:Edit"] = "Chỉnh sửa bản ghi minh họa",
                ["DemoRecords:Delete"] = "Xóa bản ghi minh họa",
                ["DemoRecords:Name"] = "Tên",
                ["DemoRecords:Description"] = "Mô tả",
                ["DemoRecords:Actions"] = "Thao tác",
                ["DemoRecords:Save"] = "Lưu",
                ["DemoRecords:Cancel"] = "Hủy",
                ["DemoRecords:BackToList"] = "Quay lại danh sách",
                ["DemoRecords:NoRecords"] = "Không có bản ghi minh họa nào.",
                ["DemoRecords:DeleteConfirmation"] = "Bạn có chắc chắn muốn xóa bản ghi minh họa này không?",
                ["DemoRecords:Previous"] = "Trước",
                ["DemoRecords:Next"] = "Sau",
                ["DemoRecords:OperationFailed"] = "Không thể hoàn tất yêu cầu. Vui lòng sửa các giá trị được đánh dấu rồi thử lại."
            }
        };

        foreach (var (cultureName, texts) in expectedTexts)
        {
            localizationOptions.Languages.ShouldContain(language => language.CultureName == cultureName);

            using (new CultureScope(cultureName))
            {
                foreach (var (key, value) in texts)
                {
                    var localized = localizer[key];
                    localized.ResourceNotFound.ShouldBeFalse($"{cultureName}:{key}");
                    localized.Value.ShouldBe(value, $"{cultureName}:{key}");
                }
            }
        }
    }

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
    public void Demo_records_web_module_should_depend_on_the_contracts_module()
    {
        var dependsOn = typeof(DemoRecordsWebModule).GetCustomAttribute<DependsOnAttribute>();

        dependsOn.ShouldNotBeNull();
        dependsOn.DependedTypes.ShouldContain(typeof(DemoRecordsApplicationContractsModule));
    }

    [Fact]
    public void Demo_records_web_module_should_register_its_menu_contributor()
    {
        var navigationOptions = GetRequiredService<IOptions<AbpNavigationOptions>>().Value;

        navigationOptions.MenuContributors.ShouldContain(contributor => contributor is DemoRecordsMenuContributor);
    }

    [Fact]
    public async Task Demo_records_menu_should_not_be_visible_without_the_read_permission()
    {
        var menuManager = GetRequiredService<IMenuManager>();
        var mainMenu = await menuManager.GetMainMenuAsync();

        mainMenu.FindMenuItem(DemoRecordsMenus.DemoRecords).ShouldBeNull();
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _currentCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _currentUICulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentUICulture;
        }
    }
}
