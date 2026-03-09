using EstaparParkingChallenge.Api.Parking;
using EstaparParkingChallenge.Site.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstaparParkingChallenge.Site.Controllers;

[ApiController]
public class RevenueController(
	IParkingService parkingService
) : ControllerBase {

	[HttpGet("revenue")]
	[AllowAnonymous]
	public async Task<ActionResult<RevenueResponse>> Revenue([FromQuery] DateOnly date, [FromQuery] string sector) {
		if (string.IsNullOrWhiteSpace(sector)) {
			return BadRequest(new { message = "Query parameter 'sector' is required." });
		}

		var response = await parkingService.GetRevenueAsync(date, sector);
		return Ok(response);
	}
}
