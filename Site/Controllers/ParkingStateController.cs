using EstaparParkingChallenge.Site.Entities;
using EstaparParkingChallenge.Site.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/parking")]
public class ParkingStateController(
	AppDbContext dbContext,
	IGarageSyncService garageSyncService,
	ILogger<ParkingStateController> logger
) : ControllerBase {

	private static readonly Action<ILogger, Exception?> logGarageSyncOnDemandFailed =
		LoggerMessage.Define(LogLevel.Warning, new EventId(5300, nameof(logGarageSyncOnDemandFailed)), "On-demand garage synchronization failed while loading parking state");

	[HttpGet("state")]
	public async Task<IActionResult> GetState(CancellationToken cancellationToken) {
		await ensureGarageDataLoadedAsync(cancellationToken);

		var sectors = await dbContext.GarageSectors
			.AsNoTracking()
			.Include(x => x.Spots)
			.OrderBy(x => x.Sector)
			.ToListAsync(cancellationToken);

		var activeSessions = await dbContext.ParkingSessions
			.AsNoTracking()
			.Where(x => x.ExitTime == null)
			.ToListAsync(cancellationToken);

		var activeParkedSessionsBySpot = activeSessions
			.Where(x => x.IsParked && x.SpotId.HasValue)
			.GroupBy(x => x.SpotId!.Value)
			.ToDictionary(
				x => x.Key,
				x => x.OrderByDescending(y => y.EntryTime).First()
			);

		var unparkedBySector = activeSessions
			.Where(x => !x.IsParked)
			.GroupBy(x => x.Sector)
			.ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

		var response = new {
			timestamp = DateTimeOffset.UtcNow,
			sectors = sectors.Select(sector => {
				var occupiedCount = sector.Spots.Count(x => x.IsOccupied);
				var availableCount = Math.Max(0, sector.MaxCapacity - occupiedCount);
				unparkedBySector.TryGetValue(sector.Sector, out var pendingCount);

				return new {
					sector = sector.Sector,
					basePrice = sector.BasePrice,
					capacity = sector.MaxCapacity,
					occupiedCount,
					availableCount,
					entryPendingCount = pendingCount,
					spots = sector.Spots
						.OrderBy(x => x.Id)
						.Select(spot => {
							activeParkedSessionsBySpot.TryGetValue(spot.Id, out var parkedSession);

							return new {
								id = spot.Id,
								lat = spot.Latitude,
								lng = spot.Longitude,
								isOccupied = spot.IsOccupied,
								licensePlate = spot.OccupiedByLicensePlate,
								entryTime = parkedSession?.EntryTime,
								state = spot.IsOccupied ? "Occupied" : "Available",
							};
						}),
				};
			}),
		};

		return Ok(response);
	}

	[HttpPost("sync")]
	public async Task<IActionResult> Sync([FromQuery] bool forceRefresh = true, CancellationToken cancellationToken = default) {
		var synced = await garageSyncService.SyncAsync(forceRefresh, cancellationToken);
		return Ok(new {
			synced,
			forceRefresh,
			timestamp = DateTimeOffset.UtcNow,
		});
	}

	private async Task ensureGarageDataLoadedAsync(CancellationToken cancellationToken) {
		var hasGarageData = await dbContext.GarageSectors.AnyAsync(cancellationToken);
		if (hasGarageData) {
			return;
		}

		try {
			await garageSyncService.SyncAsync(cancellationToken);
		} catch (Exception e) {
			logGarageSyncOnDemandFailed(logger, e);
		}
	}
}
