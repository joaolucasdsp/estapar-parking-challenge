using EstaparParkingChallenge.Site.Configuration;
using EstaparParkingChallenge.Site.Entities;
using EstaparParkingChallenge.Site.Services.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EstaparParkingChallenge.Site.Services;

public interface IGarageSyncService {
	Task<bool> SyncAsync(CancellationToken cancellationToken = default);
	Task<bool> SyncAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

public class GarageSyncService(
	ISimulatorClientService simulatorClientService,
	IOptions<SimulatorClientConfig> simulatorClientConfig,
	AppDbContext dbContext,
	ILogger<GarageSyncService> logger
) : IGarageSyncService {
	private static readonly SemaphoreSlim syncLock = new(1, 1);

	private static readonly Action<ILogger, string, Exception?> logGarageSyncStarting =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(5000, nameof(logGarageSyncStarting)), "Starting garage synchronization from {GarageEndpoint}");
	private static readonly Action<ILogger, int, int, Exception?> logGarageSyncCompleted =
		LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(5001, nameof(logGarageSyncCompleted)), "Garage synchronization completed. Sectors: {SectorCount}, spots: {SpotCount}");
	private static readonly Action<ILogger, Exception?> logGarageSyncMissingPayload =
		LoggerMessage.Define(LogLevel.Warning, new EventId(5002, nameof(logGarageSyncMissingPayload)), "Garage synchronization response was empty");
	private static readonly Action<ILogger, Exception?> logGarageSyncUsingMock =
		LoggerMessage.Define(LogLevel.Warning, new EventId(5003, nameof(logGarageSyncUsingMock)), "Simulator unavailable. Using mock garage configuration");

	public async Task<bool> SyncAsync(CancellationToken cancellationToken = default) {
		return await SyncAsync(forceRefresh: false, cancellationToken);
	}

	public async Task<bool> SyncAsync(bool forceRefresh = false, CancellationToken cancellationToken = default) {
		await syncLock.WaitAsync(cancellationToken);
		try {
		logGarageSyncStarting(logger, simulatorClientConfig.Value.GarageEndpoint, null);

		GarageConfigurationResponse? payload;
		try {
			payload = await simulatorClientService.GetGarageConfigurationAsync(forceRefresh, cancellationToken);
		} catch (Exception) when (simulatorClientConfig.Value.UseMockGarageOnFailure) {
			logGarageSyncUsingMock(logger, null);
			payload = createMockGarageConfiguration();
		}

		if (payload == null && simulatorClientConfig.Value.UseMockGarageOnFailure) {
			logGarageSyncUsingMock(logger, null);
			payload = createMockGarageConfiguration();
		}

		if (payload == null) {
			logGarageSyncMissingPayload(logger, null);
			return false;
		}

		var payloadSectorCodes = payload.Garage
			.Select(x => x.Sector)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var payloadSpotIds = payload.Spots.Select(x => x.Id).ToHashSet();

		var existingSectors = await dbContext.GarageSectors.ToDictionaryAsync(x => x.Sector, cancellationToken);
		foreach (var sector in payload.Garage) {
			if (existingSectors.TryGetValue(sector.Sector, out var existingSector)) {
				existingSector.BasePrice = sector.BasePrice;
				existingSector.MaxCapacity = sector.MaxCapacity;
			} else {
				await dbContext.GarageSectors.AddAsync(new GarageSectorEntity {
					Sector = sector.Sector,
					BasePrice = sector.BasePrice,
					MaxCapacity = sector.MaxCapacity,
				}, cancellationToken);
			}
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		var sectorIdsByCode = await dbContext.GarageSectors
			.ToDictionaryAsync(x => x.Sector, x => x.Id, cancellationToken);

		var existingSpots = await dbContext.GarageSpots.ToDictionaryAsync(x => x.Id, cancellationToken);
		foreach (var spot in payload.Spots) {
			if (!sectorIdsByCode.TryGetValue(spot.Sector, out var garageSectorId)) {
				continue;
			}

			if (existingSpots.TryGetValue(spot.Id, out var existingSpot)) {
				existingSpot.GarageSectorId = garageSectorId;
				existingSpot.Latitude = spot.Latitude;
				existingSpot.Longitude = spot.Longitude;
			} else {
				await dbContext.GarageSpots.AddAsync(new GarageSpotEntity {
					Id = spot.Id,
					GarageSectorId = garageSectorId,
					Latitude = spot.Latitude,
					Longitude = spot.Longitude,
					IsOccupied = false,
				}, cancellationToken);
			}
		}

		await dbContext.SaveChangesAsync(cancellationToken);

		var obsoleteSpots = await dbContext.GarageSpots
			.Where(x => !payloadSpotIds.Contains(x.Id))
			.ToListAsync(cancellationToken);
		if (obsoleteSpots.Count != 0) {
			dbContext.GarageSpots.RemoveRange(obsoleteSpots);
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		var obsoleteSectors = await dbContext.GarageSectors
			.Where(x => !payloadSectorCodes.Contains(x.Sector))
			.ToListAsync(cancellationToken);
		if (obsoleteSectors.Count != 0) {
			dbContext.GarageSectors.RemoveRange(obsoleteSectors);
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		logGarageSyncCompleted(logger, payload.Garage.Count, payload.Spots.Count, null);
		return true;
		} finally {
			syncLock.Release();
		}
	}

	private static GarageConfigurationResponse createMockGarageConfiguration() {
		var garage = new List<GarageSectorResponse> {
			new() {
				Sector = "A",
				BasePrice = 10m,
				MaxCapacity = 40,
			},
			new() {
				Sector = "B",
				BasePrice = 12m,
				MaxCapacity = 35,
			},
			new() {
				Sector = "C",
				BasePrice = 15m,
				MaxCapacity = 25,
			},
		};

		var sectorAnchors = new Dictionary<string, (decimal latitude, decimal longitude)> {
			["A"] = (-23.561684m, -46.655981m),
			["B"] = (-23.562384m, -46.656681m),
			["C"] = (-23.563084m, -46.657381m),
		};

		var spots = new List<GarageSpotResponse>();
		var spotId = 1;
		foreach (var sector in garage) {
			var (latitude, longitude) = sectorAnchors[sector.Sector];
			for (var i = 0; i < sector.MaxCapacity; i++) {
				var row = i / 10;
				var column = i % 10;

				spots.Add(new GarageSpotResponse {
					Id = spotId,
					Sector = sector.Sector,
					Latitude = latitude + (row * 0.00010m),
					Longitude = longitude + (column * 0.00008m),
				});

				spotId++;
			}
		}

		return new GarageConfigurationResponse {
			Garage = garage,
			Spots = spots,
		};
	}
}
