using System;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

[Authorize(DemoRecordsPermissions.Read)]
public class DetailsModel : DemoRecordsPageModel
{
    private readonly IDemoRecordAppService _demoRecordAppService;
    private readonly IPermissionChecker _permissionChecker;

    public DetailsModel(IDemoRecordAppService demoRecordAppService, IPermissionChecker permissionChecker)
    {
        _demoRecordAppService = demoRecordAppService;
        _permissionChecker = permissionChecker;
    }

    public DemoRecordDto Record { get; private set; } = new();
    public bool CanUpdate { get; private set; }
    public bool CanDelete { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var denied = DenyTenantAccess();
        if (denied is not null)
        {
            return denied;
        }

        Record = await _demoRecordAppService.GetAsync(id);
        CanUpdate = await _permissionChecker.IsGrantedAsync(DemoRecordsPermissions.Update);
        CanDelete = await _permissionChecker.IsGrantedAsync(DemoRecordsPermissions.Delete);
        return Page();
    }
}
