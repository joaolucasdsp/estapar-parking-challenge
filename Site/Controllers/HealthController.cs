using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstaparParkingChallenge.Site.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController : ControllerBase {

	[HttpGet]
	public IActionResult Get()
		=> Ok(new {
			status = "ok",
			timestamp = DateTimeOffset.UtcNow,
		});
}
