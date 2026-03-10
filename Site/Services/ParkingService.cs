using EstaparParkingChallenge.Api.Parking;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Services;

public interface IParkingService {
	Task<RevenueResponse> GetRevenueAsync(DateOnly revenueDate, string sector, CancellationToken cancellationToken = default);
}

public class ParkingService(
	AppDbContext dbContext,
	IGarageSyncService garageSyncService,
	ILogger<ParkingService> logger
) : IParkingService {

	private static readonly Action<ILogger, Exception?> logGarageSyncOnDemandFailed =
		LoggerMessage.Define(LogLevel.Warning, new EventId(5101, nameof(logGarageSyncOnDemandFailed)), "On-demand garage synchronization failed");

	public async Task<RevenueResponse> GetRevenueAsync(DateOnly revenueDate, string sector, CancellationToken cancellationToken = default) {
		await ensureGarageDataLoadedAsync(cancellationToken);

		var normalizedSector = sector.Trim().ToUpperInvariant();
		var utcStart = new DateTimeOffset(DateTime.SpecifyKind(revenueDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
		var utcEnd = utcStart.AddDays(1);

		var amount = await dbContext.ParkingSessions
			.AsNoTracking()
			.Where(x => x.GarageSectorId != null && x.ExitTime != null && x.ExitTime >= utcStart && x.ExitTime < utcEnd)
			.Where(x => x.GarageSector!.Sector == normalizedSector)
			.SumAsync(x => x.AmountCharged ?? 0m, cancellationToken);

		return new RevenueResponse {
			Amount = decimal.Round(amount, 2),
			Currency = "BRL",
			Timestamp = DateTimeOffset.UtcNow,
		};
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
