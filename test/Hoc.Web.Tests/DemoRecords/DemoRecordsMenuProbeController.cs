using System.Linq;
using System.Threading.Tasks;
using Hoc.DemoRecords.Web.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.UI.Navigation;

namespace Hoc.DemoRecords.Tests;

[AllowAnonymous]
[Route("_test/demorecords-menu")]
public class DemoRecordsMenuProbeController : ControllerBase
{
    private readonly IMenuManager _menuManager;

    public DemoRecordsMenuProbeController(IMenuManager menuManager)
    {
        _menuManager = menuManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var menu = await _menuManager.GetMainMenuAsync();
        var items = menu.Items
            .Where(item => item.Name == DemoRecordsMenus.DemoRecords)
            .Select(item => new { item.Name, item.Url })
            .ToList();

        return Ok(new { items });
    }
}
