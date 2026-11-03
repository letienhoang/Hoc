using Hoc.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Hoc.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class HocController : AbpControllerBase
{
    protected HocController()
    {
        LocalizationResource = typeof(HocResource);
    }
}
