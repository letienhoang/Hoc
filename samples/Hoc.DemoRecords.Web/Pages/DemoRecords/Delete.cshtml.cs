using System;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

[Authorize(DemoRecordsPermissions.Delete)]
[AutoValidateAntiforgeryToken]
public class DeleteModel : DemoRecordsPageModel
{
    private readonly IDemoRecordAppService _demoRecordAppService;

    public DeleteModel(IDemoRecordAppService demoRecordAppService)
    {
        _demoRecordAppService = demoRecordAppService;
    }

    public DemoRecordDto Record { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var denied = DenyTenantAccess();
        if (denied is not null)
        {
            return denied;
        }

        Record = await _demoRecordAppService.GetAsync(id);
        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(Guid id)
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

        await _demoRecordAppService.DeleteAsync(id);
        return RedirectToPage("/DemoRecords/Index");
    }
}
