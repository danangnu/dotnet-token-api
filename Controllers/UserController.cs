using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_token_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public() =>
            Ok("This is a public endpoint. No login required.");

        [Authorize]
        [HttpGet("secure")]
        public IActionResult Secure() =>
            Ok($"Hello {User.Identity?.Name}, this is a protected endpoint.");
    }
}
