using Microsoft.Extensions.Localization;
using Hoc.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Hoc.Web;

[Dependency(ReplaceServices = true)]
public class HocBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<HocResource> _localizer;

    public HocBrandingProvider(IStringLocalizer<HocResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
