using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Hoc.DemoRecords.Tests;

[Authorize(AuthenticationSchemes = "Identity.Application")]
[Route("_test/demorecords-security-context")]
public class DemoRecordsSecurityContextProbeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPermissionChecker _permissionChecker;

    public DemoRecordsSecurityContextProbeController(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IPermissionChecker permissionChecker)
    {
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var permissions = new Dictionary<string, bool>
        {
            [Permissions.DemoRecordsPermissions.Read] = await _permissionChecker.IsGrantedAsync(Permissions.DemoRecordsPermissions.Read),
            [Permissions.DemoRecordsPermissions.Create] = await _permissionChecker.IsGrantedAsync(Permissions.DemoRecordsPermissions.Create),
            [Permissions.DemoRecordsPermissions.Update] = await _permissionChecker.IsGrantedAsync(Permissions.DemoRecordsPermissions.Update),
            [Permissions.DemoRecordsPermissions.Delete] = await _permissionChecker.IsGrantedAsync(Permissions.DemoRecordsPermissions.Delete)
        };

        return Ok(new
        {
            currentUser = new
            {
                _currentUser.Id,
                _currentUser.TenantId,
                _currentUser.IsAuthenticated
            },
            currentTenant = new
            {
                _currentTenant.Id,
                _currentTenant.Name,
                _currentTenant.IsAvailable
            },
            permissions
        });
    }
}
