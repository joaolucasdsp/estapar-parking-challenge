using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.DataStorage;

public static class DataStorageExtensions {
	public static IServiceCollection AddDatabaseProvider(
		this IServiceCollection services,
		IConfiguration configuration
	) {

		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		var databaseProvider = configuration.GetValue<DatabaseProvider?>("Startup:Database")
			?? configuration.GetValue<DatabaseProvider>("Database");
		var connectionStringKey = DatabaseProviderFactory.GetConnectionStringKey(databaseProvider);
		var connectionString = configuration.GetConnectionString(connectionStringKey)
			?? configuration.GetConnectionString("DefaultConnection");

		if (string.IsNullOrWhiteSpace(connectionString)) {
			throw new InvalidOperationException($"Connection string for {databaseProvider} is not configured.");
		}

		var provider = DatabaseProviderFactory.Create(databaseProvider);

		// Register specific DbContext based on provider
		switch (databaseProvider) {
			case DatabaseProvider.PostgreSql:
				services.AddDbContext<PostgresDbContext>(options => provider.Configure(options, connectionString));
				services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PostgresDbContext>());
				break;

			case DatabaseProvider.SqlServer:
				services.AddDbContext<SqlServerDbContext>(options => provider.Configure(options, connectionString));
				services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<SqlServerDbContext>());
				break;

			default:
				throw new InvalidOperationException($"Unsupported database provider: {databaseProvider}");
		}

		return services;
	}
}

