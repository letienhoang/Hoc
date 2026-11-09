using System;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

[Authorize(DemoRecordsPermissions.Update)]
[AutoValidateAntiforgeryToken]
public class EditModel : DemoRecordsPageModel
{
    private readonly IDemoRecordAppService _demoRecordAppService;

    public EditModel(IDemoRecordAppService demoRecordAppService)
    {
        _demoRecordAppService = demoRecordAppService;
    }

    [BindProperty]
    public UpdateDemoRecordDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var denied = DenyTenantAccess();
        if (denied is not null)
        {
            return denied;
        }

        var record = await _demoRecordAppService.GetAsync(id);
        Input = new UpdateDemoRecordDto { Name = record.Name, Description = record.Description };
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

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _demoRecordAppService.UpdateAsync(id, Input);
        return RedirectToPage("/DemoRecords/Details", new { id });
    }
}
