using Hoc.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;

namespace Hoc.Authorization;

[Authorize(AuthenticationSchemes = "Identity.Application", Policy = IdentityPermissions.Users.Default)]
[Route("_test/authorization/identity-users")]
public class PermissionProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
