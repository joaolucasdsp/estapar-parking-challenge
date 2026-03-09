using System.Security.Cryptography;
using System.Text;

using EstaparParkingChallenge.Site.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace EstaparParkingChallenge.Site.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireWebhookSignatureAttribute : Attribute, IAsyncAuthorizationFilter {
	public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
		var config = context.HttpContext.RequestServices.GetRequiredService<IOptions<WebhookSignatureConfig>>().Value;
		if (!config.Enabled) {
			return;
		}

		if (string.IsNullOrWhiteSpace(config.Secret)) {
			setUnauthorized(context);
			return;
		}

		if (!context.HttpContext.Request.Headers.TryGetValue(config.HeaderName, out var signatureHeader)) {
			setUnauthorized(context);
			return;
		}

		if (!tryParseSignatureHeader(signatureHeader.ToString(), out var timestamp, out var providedSignature)) {
			setUnauthorized(context);
			return;
		}

		var payload = await readRequestBodyAsync(context);
		var signedPayload = $"{timestamp}.{payload}";
		var expectedSignature = computeSignature(signedPayload, config.Secret);

		if (!fixedTimeEquals(expectedSignature, providedSignature)) {
			setUnauthorized(context);
			return;
		}
	}

	private static void setUnauthorized(AuthorizationFilterContext context)
		=> context.Result = new UnauthorizedResult();

	private static async Task<string> readRequestBodyAsync(AuthorizationFilterContext context) {
		context.HttpContext.Request.EnableBuffering();
		using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
		var payload = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
		context.HttpContext.Request.Body.Position = 0;
		return payload;
	}

	private static bool tryParseSignatureHeader(string signatureHeader, out long timestamp, out string signatureHex) {
		timestamp = 0;
		signatureHex = string.Empty;

		if (string.IsNullOrWhiteSpace(signatureHeader)) {
			return false;
		}

		var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (var part in parts) {
			if (part.StartsWith("t=", StringComparison.OrdinalIgnoreCase)) {
				if (!long.TryParse(part[2..], out timestamp)) {
					return false;
				}
			} else if (part.StartsWith("v1=", StringComparison.OrdinalIgnoreCase)) {
				signatureHex = part[3..];
			}
		}

		if (timestamp == 0) {
			return false;
		}

		return !string.IsNullOrWhiteSpace(signatureHex);
	}

	private static byte[] computeSignature(string signedPayload, string secret) {
		var key = Encoding.UTF8.GetBytes(secret);
		var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);
		using var hmac = new HMACSHA256(key);
		return hmac.ComputeHash(payloadBytes);
	}

	private static bool fixedTimeEquals(byte[] expected, string providedHex) {
		try {
			var provided = Convert.FromHexString(providedHex);
			return CryptographicOperations.FixedTimeEquals(expected, provided);
		} catch {
			return false;
		}
	}
}
