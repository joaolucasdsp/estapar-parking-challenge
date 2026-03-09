using System.Security.Cryptography;
using System.Text;

using EstaparParkingChallenge.Site.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace EstaparParkingChallenge.Site.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireWebhookSignatureAttribute : Attribute, IAsyncAuthorizationFilter {
	public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
		var config = context.HttpContext.RequestServices.GetRequiredService<IOptions<WebhookSignatureConfig>>().Value;
		if (!config.Enabled) {
			return Task.CompletedTask;
		}

		if (string.IsNullOrWhiteSpace(config.Secret)) {
			setUnauthorized(context);
			return Task.CompletedTask;
		}

		if (!context.HttpContext.Request.Headers.TryGetValue(config.HeaderName, out var providedSecret)) {
			setUnauthorized(context);
			return Task.CompletedTask;
		}

		if (!isValidSecret(config.Secret, providedSecret.ToString())) {
			setUnauthorized(context);
			return Task.CompletedTask;
		}

		return Task.CompletedTask;
	}

	private static void setUnauthorized(AuthorizationFilterContext context)
		=> context.Result = new UnauthorizedResult();

	private static bool isValidSecret(string expectedSecret, string providedSecret) {
		var expected = Encoding.UTF8.GetBytes(expectedSecret.Trim());
		var provided = Encoding.UTF8.GetBytes(providedSecret.Trim());
		return CryptographicOperations.FixedTimeEquals(expected, provided);
	}
}
