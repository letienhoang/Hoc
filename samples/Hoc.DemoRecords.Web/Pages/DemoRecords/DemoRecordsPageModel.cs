using System.Threading.Tasks;
using Hoc.DemoRecords.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

public abstract class DemoRecordsPageModel : AbpPageModel
{
    protected DemoRecordsPageModel()
    {
        LocalizationResourceType = typeof(DemoRecordsResource);
    }

    protected IActionResult? DenyTenantAccess()
    {
        return CurrentTenant.Id.HasValue || CurrentUser.TenantId.HasValue
            ? Forbid()
            : null;
    }

    protected async Task<IActionResult?> ValidateAntiforgeryAsync()
    {
        if (!Request.HasFormContentType || !(await Request.ReadFormAsync()).ContainsKey("__RequestVerificationToken"))
        {
            return BadRequest();
        }

        try
        {
            var antiforgery = HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            await antiforgery.ValidateRequestAsync(HttpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest();
        }
    }
}
