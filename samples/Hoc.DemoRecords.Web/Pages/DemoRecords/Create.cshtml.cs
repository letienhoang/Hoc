using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

[Authorize(DemoRecordsPermissions.Create)]
[AutoValidateAntiforgeryToken]
public class CreateModel : DemoRecordsPageModel
{
    private readonly IDemoRecordAppService _demoRecordAppService;

    public CreateModel(IDemoRecordAppService demoRecordAppService)
    {
        _demoRecordAppService = demoRecordAppService;
    }

    [BindProperty]
    public CreateDemoRecordDto Input { get; set; } = new();

    public IActionResult OnGet()
    {
        return DenyTenantAccess() ?? Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        var denied = DenyTenantAccess();
        if (denied is not null)
        {
            return denied;
        }

        var invalidAntiforgery = await ValidateAntiforgeryAsync();
        if (invalidAntiforgery is not null)
        {
            return invalidAntiforgery;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var created = await _demoRecordAppService.CreateAsync(Input);
        return RedirectToPage("/DemoRecords/Details", new { id = created.Id });
    }
}
