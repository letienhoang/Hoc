using System.Collections.Generic;
using System.Globalization;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hoc.EntityFrameworkCore;
using Hoc.Localization;
using Hoc.Web;
using Hoc.Web.Menus;
using Hoc.Authentication;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Validation.Localization;

namespace Hoc;

[DependsOn(
    typeof(AbpAspNetCoreTestBaseModule),
    typeof(HocWebModule),
    typeof(HocApplicationTestModule),
    typeof(HocEntityFrameworkCoreTestModule)
)]
public class HocWebTestModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<IMvcBuilder>(builder =>
        {
            builder.PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(typeof(HocWebModule).Assembly));
            builder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(AuthenticationProbeController).Assembly));
        });

        context.Services.GetPreConfigureActions<OpenIddictServerBuilder>().Clear();
        PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
        {
            options.AddDevelopmentEncryptionAndSigningCertificate = true;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(ServiceDescriptor.Transient<IAuthorizationService, DefaultAuthorizationService>());

        ConfigureLocalizationServices(context.Services);
        ConfigureNavigationServices(context.Services);
    }

    private static void ConfigureLocalizationServices(IServiceCollection services)
    {
        var cultures = new List<CultureInfo> { new CultureInfo("en"), new CultureInfo("tr") };
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        services.Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<HocResource>()
                .AddBaseTypes(
                    typeof(AbpValidationResource),
                    typeof(AbpUiResource)
                );
        });
    }

    private static void ConfigureNavigationServices(IServiceCollection services)
    {
        services.Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new HocMenuContributor());
        });
    }
}
