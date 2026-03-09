using EstaparParkingChallenge.Simulator.Configuration;
using EstaparParkingChallenge.Simulator.Models;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TargetApiConfig>(builder.Configuration.GetSection("TargetApi"));
builder.Services.Configure<GarageConfig>(builder.Configuration.GetSection("Garage"));
builder.Services.AddSingleton(sp => {
	var garageConfig = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GarageConfig>>().Value;
	var topologyMap = getTopologyMap(garageConfig);
	var activeTopology = garageConfig.ActiveTopology?.Trim();
	if (string.IsNullOrWhiteSpace(activeTopology) || !topologyMap.ContainsKey(activeTopology)) {
		activeTopology = topologyMap.Keys.First();
	}

	return new TopologySelectionState(activeTopology);
});

builder.Services.AddOpenApi("v1");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient("TargetApi", (sp, client) => {
	var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TargetApiConfig>>().Value;
	client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/'));
}).ConfigurePrimaryHttpMessageHandler(sp => {
	var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TargetApiConfig>>().Value;
	if (!config.IgnoreTlsErrors) {
		return new HttpClientHandler();
	}

	return new HttpClientHandler {
		ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
	};
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options => {
	options.Title = "Parking Challenge Simulator";
});

app.MapGet("/simulator/config", (Microsoft.Extensions.Options.IOptions<TargetApiConfig> targetApiOptions) => {
	var target = targetApiOptions.Value;
	return Results.Ok(new {
		targetApiBaseUrl = target.BaseUrl,
		webhookPath = target.WebhookPath,
		secretHeaderName = target.SecretHeaderName,
		hasSecret = !string.IsNullOrWhiteSpace(target.Secret),
	});
});

app.MapGet("/target/health", async (
	IHttpClientFactory httpClientFactory,
	CancellationToken cancellationToken
) => {
	var client = httpClientFactory.CreateClient("TargetApi");
	using var response = await client.GetAsync("/api/health", cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return Results.Ok(new {
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	});
});

app.MapGet("/target/revenue", async (
	DateOnly date,
	string sector,
	IHttpClientFactory httpClientFactory,
	CancellationToken cancellationToken
) => {
	var client = httpClientFactory.CreateClient("TargetApi");
	var path = $"/api/revenue?date={date:yyyy-MM-dd}&sector={Uri.EscapeDataString(sector)}";
	using var response = await client.GetAsync(path, cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return Results.Ok(new {
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	});
});

app.MapGet("/target/parking/state", async (
	IHttpClientFactory httpClientFactory,
	CancellationToken cancellationToken
) => {
	var client = httpClientFactory.CreateClient("TargetApi");
	using var response = await client.GetAsync("/api/parking/state", cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return Results.Ok(new {
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	});
});

app.MapPost("/target/parking/sync", async (
	IHttpClientFactory httpClientFactory,
	CancellationToken cancellationToken
) => {
	var client = httpClientFactory.CreateClient("TargetApi");
	using var response = await client.PostAsync("/api/parking/sync?forceRefresh=true", content: null, cancellationToken);
	var body = await response.Content.ReadAsStringAsync(cancellationToken);

	return Results.Ok(new {
		statusCode = (int)response.StatusCode,
		reason = response.ReasonPhrase,
		responseBody = body,
	});
});

app.MapGet("/simulator/topologies", (
	Microsoft.Extensions.Options.IOptions<GarageConfig> garageOptions,
	TopologySelectionState topologyState
) => {
	var topologyMap = getTopologyMap(garageOptions.Value);
	var active = topologyState.Get();

	return Results.Ok(new {
		activeTopology = active,
		availableTopologies = topologyMap
			.OrderBy(x => x.Key)
			.Select(x => new {
				name = x.Key,
				sectorCount = x.Value.Count,
				spotCount = x.Value.Sum(y => y.MaxCapacity),
			}),
	});
});

app.MapPost("/simulator/topologies/select", (
	TopologySelectionRequest request,
	Microsoft.Extensions.Options.IOptions<GarageConfig> garageOptions,
	TopologySelectionState topologyState
) => {
	var topologyMap = getTopologyMap(garageOptions.Value);
	var selectedName = request.Name?.Trim() ?? string.Empty;
	if (string.IsNullOrWhiteSpace(selectedName) || !topologyMap.ContainsKey(selectedName)) {
		return Results.BadRequest(new {
			message = "Invalid topology name",
			available = topologyMap.Keys.OrderBy(x => x),
		});
	}

	topologyState.Set(selectedName);
	return Results.Ok(new {
		activeTopology = selectedName,
	});
});

app.MapGet("/garage", (
	Microsoft.Extensions.Options.IOptions<GarageConfig> garageOptions,
	TopologySelectionState topologyState
) => {
	var sectors = getSectorsForTopology(garageOptions.Value, topologyState.Get());
	var response = buildGarageResponseFromSectors(sectors);
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
	TopologySelectionState topologyState,
	CancellationToken cancellationToken
) => {
	var normalizedPlate = request.LicensePlate.Trim().ToUpperInvariant();
	var entryTime = request.EntryTime ?? DateTimeOffset.UtcNow;
	var exitTime = request.ExitTime ?? entryTime.AddHours(2);

	var topologySectors = getSectorsForTopology(garageOptions.Value, topologyState.Get());
	var targetSpot = resolveSpot(request, topologySectors);
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

static GarageConfigurationResponse buildGarageResponseFromSectors(List<GarageSectorSeed> sectorSeeds) {
	var response = new GarageConfigurationResponse();
	var nextSpotId = 1;

	foreach (var (sector, sectorIndex) in sectorSeeds.OrderBy(x => x.Sector).Select((value, index) => (value, index))) {
		response.Garage.Add(new GarageSectorResponse {
			Sector = sector.Sector,
			BasePrice = sector.BasePrice,
			MaxCapacity = sector.MaxCapacity,
		});

		var baseLatitude = -23.561684m - (sectorIndex * 0.0020m);
		var baseLongitude = -46.655981m - (sectorIndex * 0.0020m);
		for (var i = 0; i < sector.MaxCapacity; i++) {
			var row = i / 10;
			var column = i % 10;

			response.Spots.Add(new GarageSpotResponse {
				Id = nextSpotId++,
				Sector = sector.Sector,
				Latitude = baseLatitude + (row * 0.00010m),
				Longitude = baseLongitude + (column * 0.00008m),
			});
		}
	}

	return response;
}

static GarageSpotResponse? resolveSpot(SimulateFlowRequest request, List<GarageSectorSeed> sectorSeeds) {
	if (request.Latitude.HasValue && request.Longitude.HasValue) {
		return new GarageSpotResponse {
			Latitude = request.Latitude.Value,
			Longitude = request.Longitude.Value,
		};
	}

	var garage = buildGarageResponseFromSectors(sectorSeeds);
	return garage.Spots.FirstOrDefault(x => x.Sector.Equals(request.Sector, StringComparison.OrdinalIgnoreCase));
}

static Dictionary<string, List<GarageSectorSeed>> getTopologyMap(GarageConfig config) {
	var topologies = config.Topologies
		.Where(x => !string.IsNullOrWhiteSpace(x.Key))
		.ToDictionary(
			x => x.Key.Trim(),
			x => x.Value ?? [],
			StringComparer.OrdinalIgnoreCase
		);

	if (topologies.Count == 0) {
		topologies["default"] = config.Sectors.Count > 0
			? config.Sectors
			: [
				new GarageSectorSeed { Sector = "A", BasePrice = 10m, MaxCapacity = 40 },
				new GarageSectorSeed { Sector = "B", BasePrice = 12m, MaxCapacity = 35 },
				new GarageSectorSeed { Sector = "C", BasePrice = 15m, MaxCapacity = 25 },
			];
	}

	return topologies;
}

static List<GarageSectorSeed> getSectorsForTopology(GarageConfig config, string? topologyName) {
	var topologyMap = getTopologyMap(config);
	var normalizedName = topologyName?.Trim() ?? string.Empty;
	if (string.IsNullOrWhiteSpace(normalizedName) || !topologyMap.TryGetValue(normalizedName, out var sectors)) {
		sectors = topologyMap.Values.First();
	}

	return sectors;
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

sealed class TopologySelectionState(string activeTopology) {
	private readonly object gate = new();
	private string activeTopology = activeTopology;

	public string Get() {
		lock (gate) {
			return activeTopology;
		}
	}

	public void Set(string topologyName) {
		lock (gate) {
			activeTopology = topologyName;
		}
	}
}
