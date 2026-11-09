using System;
using System.Threading.Tasks;
using Hoc.DemoRecords.DemoRecords;
using Hoc.DemoRecords.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;

namespace Hoc.DemoRecords.Web.Pages.DemoRecords;

[Authorize(DemoRecordsPermissions.Read)]
public class IndexModel : DemoRecordsPageModel
{
    private readonly IDemoRecordAppService _demoRecordAppService;
    private readonly IPermissionChecker _permissionChecker;

    public IndexModel(
        IDemoRecordAppService demoRecordAppService,
        IPermissionChecker permissionChecker)
    {
        _demoRecordAppService = demoRecordAppService;
        _permissionChecker = permissionChecker;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResultDto<DemoRecordDto> Records { get; private set; } = new();

    public bool CanCreate { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanDelete { get; private set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => (long)PageNumber * GetDemoRecordListInput.DefaultMaxResultCount < Records.TotalCount;

    public async Task<IActionResult> OnGetAsync()
    {
        var denied = DenyTenantAccess();
        if (denied is not null)
        {
            return denied;
        }

        PageNumber = Math.Clamp(PageNumber, 1, (int.MaxValue / GetDemoRecordListInput.DefaultMaxResultCount) + 1);
        Records = await _demoRecordAppService.GetListAsync(new GetDemoRecordListInput
        {
            SkipCount = (PageNumber - 1) * GetDemoRecordListInput.DefaultMaxResultCount,
            MaxResultCount = GetDemoRecordListInput.DefaultMaxResultCount
        });
        CanCreate = await _permissionChecker.IsGrantedAsync(DemoRecordsPermissions.Create);
        CanUpdate = await _permissionChecker.IsGrantedAsync(DemoRecordsPermissions.Update);
        CanDelete = await _permissionChecker.IsGrantedAsync(DemoRecordsPermissions.Delete);

        return Page();
    }
}
