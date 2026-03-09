using EstaparParkingChallenge.Simulator.Configuration;
using EstaparParkingChallenge.Simulator.Models;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TargetApiConfig>(builder.Configuration.GetSection("TargetApi"));
builder.Services.Configure<GarageConfig>(builder.Configuration.GetSection("Garage"));

builder.Services.AddOpenApi("v1");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient("TargetApi", (sp, client) => {
	var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TargetApiConfig>>().Value;
	client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/'));
});

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options => {
	options.Title = "Parking Challenge Simulator";
});

app.MapGet("/garage", (Microsoft.Extensions.Options.IOptions<GarageConfig> garageOptions) => {
	var response = buildGarageResponse(garageOptions.Value);
	return Results.Ok(response);
});

app.MapPost("/simulate/event", async (
	WebhookEventRequest request,
	IHttpClientFactory httpClientFactory,
	Microsoft.Extensions.Options.IOptions<TargetApiConfig> targetApiOptions,
	CancellationToken cancellationToken
) => {
	var targetApi = targetApiOptions.Value;
	var client = httpClientFactory.CreateClient("TargetApi");
	using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetApi.WebhookPath) {
		Content = JsonContent.Create(request),
	};

	if (!string.IsNullOrWhiteSpace(targetApi.Secret)) {
		httpRequest.Headers.TryAddWithoutValidation(targetApi.SecretHeaderName, targetApi.Secret);
	}

	using var response = await client.SendAsync(httpRequest, cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return Results.Ok(new {
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	});
});

app.MapPost("/simulate/flow", async (
	SimulateFlowRequest request,
	IHttpClientFactory httpClientFactory,
	Microsoft.Extensions.Options.IOptions<TargetApiConfig> targetApiOptions,
	Microsoft.Extensions.Options.IOptions<GarageConfig> garageOptions,
	CancellationToken cancellationToken
) => {
	var normalizedPlate = request.LicensePlate.Trim().ToUpperInvariant();
	var entryTime = request.EntryTime ?? DateTimeOffset.UtcNow;
	var exitTime = request.ExitTime ?? entryTime.AddHours(2);

	var targetSpot = resolveSpot(request, garageOptions.Value);
	var entryEvent = new WebhookEventRequest {
		LicensePlate = normalizedPlate,
		EntryTime = entryTime,
		EventType = "ENTRY",
	};
	var parkedEvent = new WebhookEventRequest {
		LicensePlate = normalizedPlate,
		Latitude = targetSpot?.Latitude,
		Longitude = targetSpot?.Longitude,
		EventType = "PARKED",
	};
	var exitEvent = new WebhookEventRequest {
		LicensePlate = normalizedPlate,
		ExitTime = exitTime,
		EventType = "EXIT",
	};

	var entryResult = await sendWebhookAsync(entryEvent, httpClientFactory, targetApiOptions.Value, cancellationToken);
	var parkedResult = await sendWebhookAsync(parkedEvent, httpClientFactory, targetApiOptions.Value, cancellationToken);
	var exitResult = await sendWebhookAsync(exitEvent, httpClientFactory, targetApiOptions.Value, cancellationToken);

	return Results.Ok(new {
		plate = normalizedPlate,
		entry = entryResult,
		parked = parkedResult,
		exit = exitResult,
	});
});

app.Run();

static GarageConfigurationResponse buildGarageResponse(GarageConfig config) {
	var response = new GarageConfigurationResponse();
	var nextSpotId = 1;

	foreach (var sector in config.Sectors.OrderBy(x => x.Sector)) {
		response.Garage.Add(new GarageSectorResponse {
			Sector = sector.Sector,
			BasePrice = sector.BasePrice,
			MaxCapacity = sector.MaxCapacity,
		});

		var baseLatitude = -23.561684m - ((nextSpotId % 5) * 0.0001m);
		var baseLongitude = -46.655981m - ((nextSpotId % 7) * 0.0001m);
		for (var i = 0; i < sector.MaxCapacity; i++) {
			response.Spots.Add(new GarageSpotResponse {
				Id = nextSpotId++,
				Sector = sector.Sector,
				Latitude = baseLatitude + (i / 10m) * 0.0001m,
				Longitude = baseLongitude + (i % 10m) * 0.00008m,
			});
		}
	}

	return response;
}

static GarageSpotResponse? resolveSpot(SimulateFlowRequest request, GarageConfig config) {
	if (request.Latitude.HasValue && request.Longitude.HasValue) {
		return new GarageSpotResponse {
			Latitude = request.Latitude.Value,
			Longitude = request.Longitude.Value,
		};
	}

	var garage = buildGarageResponse(config);
	return garage.Spots.FirstOrDefault(x => x.Sector.Equals(request.Sector, StringComparison.OrdinalIgnoreCase));
}

static async Task<object> sendWebhookAsync(WebhookEventRequest payload, IHttpClientFactory httpClientFactory, TargetApiConfig targetApi, CancellationToken cancellationToken) {
	var client = httpClientFactory.CreateClient("TargetApi");
	using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetApi.WebhookPath) {
		Content = JsonContent.Create(payload),
	};

	if (!string.IsNullOrWhiteSpace(targetApi.Secret)) {
		httpRequest.Headers.TryAddWithoutValidation(targetApi.SecretHeaderName, targetApi.Secret);
	}

	using var response = await client.SendAsync(httpRequest, cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return new {
		eventType = payload.EventType,
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	};
}
