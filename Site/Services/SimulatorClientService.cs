using System.Text.Json;

using EstaparParkingChallenge.Site.Configuration;
using EstaparParkingChallenge.Site.Services.Models;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EstaparParkingChallenge.Site.Services;

public interface ISimulatorClientService {
	Task<GarageConfigurationResponse?> GetGarageConfigurationAsync(CancellationToken cancellationToken = default);
}

public class SimulatorClientService(
	IHttpClientFactory httpClientFactory,
	IOptions<SimulatorClientConfig> simulatorClientConfig,
	IDistributedCache distributedCache,
	ILogger<SimulatorClientService> logger
) : ISimulatorClientService {

	public const string ClientName = "SimulatorClient";

	private static readonly Action<ILogger, string, Exception?> logGarageCacheHit =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(5200, nameof(logGarageCacheHit)), "Garage configuration cache hit for key {CacheKey}");
	private static readonly Action<ILogger, string, Exception?> logGarageCacheUpdated =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(5201, nameof(logGarageCacheUpdated)), "Garage configuration cache updated for key {CacheKey}");
	private static readonly Action<ILogger, string, Exception?> logGarageCacheReadFailed =
		LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5202, nameof(logGarageCacheReadFailed)), "Garage configuration cache read failed for key {CacheKey}");
	private static readonly Action<ILogger, string, Exception?> logGarageCacheWriteFailed =
		LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5203, nameof(logGarageCacheWriteFailed)), "Garage configuration cache write failed for key {CacheKey}");

	public async Task<GarageConfigurationResponse?> GetGarageConfigurationAsync(CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(simulatorClientConfig.Value.GarageBaseUrl)) {
			return null;
		}

		var endpoint = simulatorClientConfig.Value.GarageEndpoint?.Trim() ?? "/garage";
		if (!endpoint.StartsWith('/')) {
			endpoint = $"/{endpoint}";
		}

		var cacheKey = $"simulator:garage:{simulatorClientConfig.Value.GarageBaseUrl.TrimEnd('/')}{endpoint}";
		GarageConfigurationResponse? cachedPayload = null;
		try {
			var cachedJson = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
			if (!string.IsNullOrWhiteSpace(cachedJson)) {
				cachedPayload = JsonSerializer.Deserialize<GarageConfigurationResponse>(cachedJson);
			}
		} catch (Exception e) {
			logGarageCacheReadFailed(logger, cacheKey, e);
		}

		if (cachedPayload != null) {
			logGarageCacheHit(logger, cacheKey, null);
			return cachedPayload;
		}

		var client = httpClientFactory.CreateClient(ClientName);
		var payload = await client.GetFromJsonAsync<GarageConfigurationResponse>(endpoint, cancellationToken);
		if (payload != null) {
			var cacheDuration = TimeSpan.FromMinutes(Math.Max(1, simulatorClientConfig.Value.GarageConfigCacheMinutes));
			try {
				await distributedCache.SetStringAsync(
					cacheKey,
					JsonSerializer.Serialize(payload),
					new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheDuration },
					cancellationToken
				);
			} catch (Exception e) {
				logGarageCacheWriteFailed(logger, cacheKey, e);
			}
			logGarageCacheUpdated(logger, cacheKey, null);
		}

		return payload;
	}
}
