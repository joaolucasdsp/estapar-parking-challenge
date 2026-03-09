using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Entities;

public class AppDbContext(
	DbContextOptions options
) : DbContext(options) {
	public DbSet<GarageSectorEntity> GarageSectors => Set<GarageSectorEntity>();
	public DbSet<GarageSpotEntity> GarageSpots => Set<GarageSpotEntity>();
	public DbSet<ParkingSessionEntity> ParkingSessions => Set<ParkingSessionEntity>();
	public DbSet<ParkingWebhookEventEntity> ParkingWebhookEvents => Set<ParkingWebhookEventEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<GarageSectorEntity>(entity => {
			entity.HasIndex(x => x.Sector).IsUnique();
		});

		modelBuilder.Entity<GarageSpotEntity>(entity => {
			entity.HasOne(x => x.GarageSector)
				.WithMany(x => x.Spots)
				.HasForeignKey(x => x.GarageSectorId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasIndex(x => new { x.Latitude, x.Longitude });
		});

		modelBuilder.Entity<ParkingSessionEntity>(entity => {
			entity.HasIndex(x => new { x.LicensePlate, x.ExitTime });
			entity.HasIndex(x => new { x.Sector, x.ExitTime });
		});

		modelBuilder.Entity<ParkingWebhookEventEntity>(entity => {
			entity.HasIndex(x => x.IdempotencyKey).IsUnique();
			entity.HasIndex(x => x.LicensePlate);
		});
	}
}

public sealed class PostgresDbContext(DbContextOptions<PostgresDbContext> options) : AppDbContext(options) { }

public sealed class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : AppDbContext(options) { }

