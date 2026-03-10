using EstaparParkingChallenge.Api.Parking;
using EstaparParkingChallenge.Site.Filters;
using EstaparParkingChallenge.Site.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstaparParkingChallenge.Site.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhook")]
[Route("webhook")]
public class WebhookController(
	IWebhookProcessingService webhookProcessingService
) : ControllerBase {

	[HttpPost]
	[RequireWebhookSignature]
	public async Task<IActionResult> Webhook([FromBody] WebhookEventRequest request, CancellationToken cancellationToken) {
		await webhookProcessingService.HandleWebhookEventAsync(request, cancellationToken);
		return Ok();
	}
}
