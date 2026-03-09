using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.DataStorage;

public class PostgresDatabaseProvider : IDatabaseProvider {
	public void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString) {
		optionsBuilder.UseNpgsql(connectionString, options => {
			options.MigrationsHistoryTable("__EFMigrationsHistory");
			options.EnableRetryOnFailure(maxRetryCount: 3);
			options.CommandTimeout(30);
		});

		optionsBuilder.EnableSensitiveDataLogging(false);
		optionsBuilder.EnableDetailedErrors(false);
	}
}
