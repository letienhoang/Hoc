using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hoc.Authentication;

[Authorize(AuthenticationSchemes = "Identity.Application")]
[Route("_test/authentication-probe")]
public class AuthenticationProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
