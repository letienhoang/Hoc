using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Hoc.DemoRecords.Web.Menus;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Web;

[DependsOn(
    typeof(DemoRecordsApplicationContractsModule),
    typeof(AbpUiNavigationModule)
)]
public class DemoRecordsWebModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddRazorPages(options =>
            {
                options.Conventions.AddPageApplicationModelConvention(
                    "/DemoRecords/Create",
                    model => model.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
                options.Conventions.AddPageApplicationModelConvention(
                    "/DemoRecords/Edit",
                    model => model.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
                options.Conventions.AddPageApplicationModelConvention(
                    "/DemoRecords/Delete",
                    model => model.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
            })
            .AddApplicationPart(typeof(DemoRecordsWebModule).Assembly);

        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new DemoRecordsMenuContributor());
        });
    }
}
