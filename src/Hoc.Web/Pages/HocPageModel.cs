using Hoc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Hoc.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class HocPageModel : AbpPageModel
{
    protected HocPageModel()
    {
        LocalizationResourceType = typeof(HocResource);
    }
}
