using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.DataStorage.Factories;

internal static class DesignTimeDbContextFactoryHelper {
	public static DbContextOptions<TContext> CreateOptions<TContext>(DatabaseProvider databaseProvider)
		where TContext : DbContext {
		var configuration = buildConfiguration();
		var connectionStringKey = DatabaseProviderFactory.GetConnectionStringKey(databaseProvider);
		var connectionString = configuration.GetConnectionString(connectionStringKey)
			?? configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException($"Connection string for {databaseProvider} is not configured.");

		var optionsBuilder = new DbContextOptionsBuilder<TContext>();
		var provider = DatabaseProviderFactory.Create(databaseProvider);
		provider.Configure(optionsBuilder, connectionString);

		return optionsBuilder.Options;
	}

	private static IConfiguration buildConfiguration() {
		var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

		return new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
			.AddUserSecrets<AppDbContext>(optional: true)
			.AddEnvironmentVariables()
			.Build();
	}
}
