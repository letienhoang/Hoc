using System.Linq;
using System.Threading.Tasks;
using Hoc.DemoRecords.Web.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Users;

namespace Hoc.DemoRecords.Tests;

[AllowAnonymous]
[Route("_test/demorecords-menu")]
public class DemoRecordsMenuProbeController : ControllerBase
{
    private readonly IMenuManager _menuManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public DemoRecordsMenuProbeController(
        IMenuManager menuManager,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser)
    {
        _menuManager = menuManager;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var menu = await _menuManager.GetMainMenuAsync();
        var items = menu.Items
            .Where(item => item.Name == DemoRecordsMenus.DemoRecords)
            .Select(item => new { item.Name, item.Url })
            .ToList();

        return Ok(new
        {
            items,
            currentUser = new { _currentUser.Id, _currentUser.TenantId, _currentUser.IsAuthenticated },
            currentTenant = new { _currentTenant.Id, _currentTenant.IsAvailable }
        });
    }
}
