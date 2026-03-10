using EstaparParkingChallenge.Api;
using EstaparParkingChallenge.Api.Parking;
using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace EstaparParkingChallenge.Site.Services;

public interface IWebhookProcessingService {
	Task HandleWebhookEventAsync(WebhookEventRequest request, CancellationToken cancellationToken = default);
}

public class WebhookProcessingService(
	AppDbContext dbContext,
	IGarageSyncService garageSyncService,
	IParkingPricingService parkingPricingService,
	ILogger<WebhookProcessingService> logger
) : IWebhookProcessingService {

	private static readonly Action<ILogger, string, string, Exception?> logIgnoredEvent =
		LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(5100, nameof(logIgnoredEvent)), "Ignored webhook event {EventType} for plate {LicensePlate}");
	private static readonly Action<ILogger, Exception?> logGarageSyncOnDemandFailed =
		LoggerMessage.Define(LogLevel.Warning, new EventId(5101, nameof(logGarageSyncOnDemandFailed)), "On-demand garage synchronization failed");

	public async Task HandleWebhookEventAsync(WebhookEventRequest request, CancellationToken cancellationToken = default) {
		await ensureGarageDataLoadedAsync(cancellationToken);

		var normalizedPlate = request.LicensePlate.Trim().ToUpperInvariant();
		var eventType = parseEventType(request.EventType);
		var session = await dbContext.ParkingSessions
			.OrderByDescending(x => x.EntryTime)
			.FirstOrDefaultAsync(x => x.LicensePlate == normalizedPlate && x.ExitTime == null, cancellationToken);
		var eventTime = getEventTime(request, eventType);
		var idempotencyKey = buildIdempotencyKey(normalizedPlate, eventType, eventTime, request.Latitude, request.Longitude, session);

		await processEventAsync(eventType, normalizedPlate, request, eventTime, session, cancellationToken);
		await saveProcessedEventAsync(idempotencyKey, normalizedPlate, eventType, eventTime, cancellationToken);

		try {
			await dbContext.SaveChangesAsync(cancellationToken);
		} catch (DbUpdateException e) when (isIdempotencyConflict(e)) {
			// Concurrent duplicate webhook: unique index enforces idempotency.
			dbContext.ChangeTracker.Clear();
			ignoreEvent(eventType, normalizedPlate);
		}
	}

	private async Task processEventAsync(ParkingEventType eventType, string normalizedPlate, WebhookEventRequest request, DateTimeOffset eventTime, ParkingSessionEntity? session, CancellationToken cancellationToken) {
		switch (eventType) {
			case ParkingEventType.Entry:
				await handleEntryAsync(normalizedPlate, eventTime, session, cancellationToken);
				return;
			case ParkingEventType.Parked:
				await handleParkedAsync(normalizedPlate, request.Latitude, request.Longitude, session, cancellationToken);
				return;
			case ParkingEventType.Exit:
				await handleExitAsync(eventTime, session, cancellationToken);
				return;
			default:
				throw new ApiException(Api.ErrorCodes.ValidationError, "Unsupported event type");
		}
	}

	private async Task saveProcessedEventAsync(string idempotencyKey, string normalizedPlate, ParkingEventType eventType, DateTimeOffset eventTime, CancellationToken cancellationToken) {
		await dbContext.ParkingWebhookEvents.AddAsync(new ParkingWebhookEventEntity {
			Id = Guid.NewGuid(),
			IdempotencyKey = idempotencyKey,
			LicensePlate = normalizedPlate,
			EventType = eventType,
			OccurredAt = eventTime,
			ProcessedAt = DateTimeOffset.UtcNow,
		}, cancellationToken);
	}

	private async Task handleEntryAsync(string normalizedPlate, DateTimeOffset entryTime, ParkingSessionEntity? activeSession, CancellationToken cancellationToken) {
		if (activeSession != null) {
			ignoreEvent(ParkingEventType.Entry, normalizedPlate);
			return;
		}

		var totalCapacity = await dbContext.GarageSectors.SumAsync(x => x.MaxCapacity, cancellationToken);
		if (totalCapacity <= 0) {
			ignoreEvent(ParkingEventType.Entry, normalizedPlate);
			return;
		}

		var activeCount = await dbContext.ParkingSessions.CountAsync(x => x.ExitTime == null, cancellationToken);
		if (activeCount >= totalCapacity) {
			ignoreEvent(ParkingEventType.Entry, normalizedPlate);
			return;
		}

		var occupancyBeforeEntry = totalCapacity == 0 ? 0m : (decimal)activeCount / totalCapacity;
		var multiplier = parkingPricingService.GetPriceMultiplier(occupancyBeforeEntry);

		await dbContext.ParkingSessions.AddAsync(new ParkingSessionEntity {
			Id = Guid.NewGuid(),
			LicensePlate = normalizedPlate,
			EntryTime = entryTime,
			EntryPriceMultiplier = multiplier,
			BasePriceAtEntry = null,
			IsParked = false,
		}, cancellationToken);
	}

	private async Task handleParkedAsync(string normalizedPlate, decimal? latitude, decimal? longitude, ParkingSessionEntity? activeSession, CancellationToken cancellationToken) {
		if (activeSession == null || activeSession.IsParked) {
			ignoreEvent(ParkingEventType.Parked, normalizedPlate);
			return;
		}

		if (latitude == null || longitude == null) {
			ignoreEvent(ParkingEventType.Parked, normalizedPlate);
			return;
		}

		var spot = await dbContext.GarageSpots
			.Include(x => x.GarageSector)
			.FirstOrDefaultAsync(
			x => x.Latitude == latitude && x.Longitude == longitude,
			cancellationToken
		);
		if (spot == null || spot.IsOccupied) {
			ignoreEvent(ParkingEventType.Parked, normalizedPlate);
			return;
		}

		if (spot.GarageSector == null) {
			ignoreEvent(ParkingEventType.Parked, normalizedPlate);
			return;
		}

		spot.IsOccupied = true;
		spot.OccupiedByLicensePlate = normalizedPlate;
		activeSession.SpotId = spot.Id;
		activeSession.IsParked = true;
		activeSession.GarageSectorId = spot.GarageSectorId;
		activeSession.BasePriceAtEntry = spot.GarageSector.BasePrice;
	}

	private async Task handleExitAsync(DateTimeOffset exitTime, ParkingSessionEntity? activeSession, CancellationToken cancellationToken) {
		if (activeSession == null) {
			ignoreEvent(ParkingEventType.Exit, "unknown");
			return;
		}

		if (!activeSession.IsParked || !activeSession.SpotId.HasValue || !activeSession.GarageSectorId.HasValue || !activeSession.BasePriceAtEntry.HasValue) {
			ignoreEvent(ParkingEventType.Exit, activeSession.LicensePlate);
			return;
		}

		var spot = await dbContext.GarageSpots.FirstOrDefaultAsync(x => x.Id == activeSession.SpotId.Value, cancellationToken);
		if (spot != null) {
			spot.IsOccupied = false;
			spot.OccupiedByLicensePlate = null;
		}

		activeSession.ExitTime = exitTime;
		activeSession.AmountCharged = parkingPricingService.CalculateAmount(
			activeSession.EntryTime,
			activeSession.ExitTime,
			activeSession.BasePriceAtEntry.Value,
			activeSession.EntryPriceMultiplier
		);
	}

	private static DateTimeOffset getEventTime(WebhookEventRequest request, ParkingEventType eventType) {
		return eventType switch {
			ParkingEventType.Entry => request.EntryTime ?? DateTimeOffset.UtcNow,
			ParkingEventType.Parked => DateTimeOffset.UtcNow,
			ParkingEventType.Exit => request.ExitTime ?? DateTimeOffset.UtcNow,
			_ => DateTimeOffset.UtcNow,
		};
	}

	private static ParkingEventType parseEventType(string eventType) {
		if (Enum.TryParse<ParkingEventType>(eventType, true, out var parsed) && Enum.IsDefined(parsed)) {
			return parsed;
		}

		throw new ApiException(Api.ErrorCodes.ValidationError, "Invalid event_type", new { EventType = eventType });
	}

	private static string buildIdempotencyKey(string licensePlate, ParkingEventType eventType, DateTimeOffset eventTime, decimal? latitude, decimal? longitude, ParkingSessionEntity? session) {
		var lat = latitude?.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) ?? "-";
		var lng = longitude?.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) ?? "-";

		if (eventType == ParkingEventType.Parked) {
			var sessionKey = session?.Id.ToString() ?? "no-session";
			return $"{licensePlate}|{eventType}|{sessionKey}|{lat}|{lng}";
		}

		return $"{licensePlate}|{eventType}|{eventTime.ToUnixTimeMilliseconds()}|{lat}|{lng}";
	}

	private static bool isIdempotencyConflict(DbUpdateException exception) {
		if (exception.InnerException is PostgresException postgresException) {
			return postgresException.SqlState == "23505";
		}

		if (exception.InnerException is SqlException sqlException) {
			return sqlException.Number == 2601 || sqlException.Number == 2627;
		}

		return false;
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

	private void ignoreEvent(ParkingEventType eventType, string licensePlate)
		=> logIgnoredEvent(logger, eventType.ToString(), licensePlate, null);
}
