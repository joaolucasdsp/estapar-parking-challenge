using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Tests.Integration;

public class TestDatabaseManager(AppDbContext dbContext) {
	private readonly AppDbContext dbContext = dbContext;

	public async Task ResetAsync(CancellationToken cancellationToken = default) {
		await dbContext.Database.EnsureDeletedAsync(cancellationToken);
		await dbContext.Database.MigrateAsync(cancellationToken);

		dbContext.ParkingWebhookEvents.RemoveRange(dbContext.ParkingWebhookEvents);
		dbContext.ParkingSessions.RemoveRange(dbContext.ParkingSessions);
		dbContext.GarageSpots.RemoveRange(dbContext.GarageSpots);
		dbContext.GarageSectors.RemoveRange(dbContext.GarageSectors);

		await dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task SeedGarageAsync(int capacity, decimal basePrice, CancellationToken cancellationToken = default) {
		var sector = new GarageSectorEntity {
			Sector = "A",
			BasePrice = basePrice,
			MaxCapacity = capacity,
		};

		dbContext.GarageSectors.Add(sector);
		await dbContext.SaveChangesAsync(cancellationToken);

		dbContext.GarageSpots.Add(new GarageSpotEntity {
			Id = 1,
			GarageSectorId = sector.Id,
			Latitude = -23.561684m,
			Longitude = -46.655981m,
			IsOccupied = false,
		});

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}
