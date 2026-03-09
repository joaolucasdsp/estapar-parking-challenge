using System.Reflection;

using CommandLine;

using EstaparParkingChallenge.Site.Configuration;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EstaparParkingChallenge.Site.Services;

public static class AppServices {

	public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration) {

		var redisConfig = configuration.GetSection(AppConfiguration.RedisSection).Get<RedisConfig>() ?? new RedisConfig();
		if (redisConfig.Enabled && !string.IsNullOrWhiteSpace(redisConfig.ConnectionString)) {
			services.AddStackExchangeRedisCache(options => {
				options.Configuration = redisConfig.ConnectionString;
			});
		} else {
			services.AddDistributedMemoryCache();
		}

		services.AddScoped<CommandService>();

		services.AddScoped<IApplicationService, ApplicationService>();
		services.AddScoped<IParkingService, ParkingService>();
		services.AddScoped<IWebhookProcessingService, WebhookProcessingService>();
		services.AddScoped<IParkingPricingService, ParkingPricingService>();
		services.AddScoped<ISimulatorClientService, SimulatorClientService>();
		services.AddScoped<IGarageSyncService, GarageSyncService>();
		services.AddHttpClient(SimulatorClientService.ClientName, (sp, client) => {
			var config = sp.GetRequiredService<IOptions<SimulatorClientConfig>>().Value;
			if (!string.IsNullOrWhiteSpace(config.GarageBaseUrl)) {
				client.BaseAddress = new Uri(config.GarageBaseUrl.TrimEnd('/'));
			}
		});

		services.AddHostedService<StartupService>();

		return services;
	}
}

public class StartupService(
	ILogger<StartupService> logger,
	IServiceScopeFactory serviceScopeFactory,
	IOptions<StartupConfig> startupConfig
) : IHostedService {

	private static readonly Action<ILogger, Exception?> logGarageSyncSkipped =
		LoggerMessage.Define(LogLevel.Information, new EventId(2005, nameof(logGarageSyncSkipped)), "Garage synchronization skipped by configuration");
	private static readonly Action<ILogger, Exception?> logGarageSyncSucceeded =
		LoggerMessage.Define(LogLevel.Information, new EventId(2006, nameof(logGarageSyncSucceeded)), "Garage synchronization completed on startup");
	private static readonly Action<ILogger, Exception?> logGarageSyncFailed =
		LoggerMessage.Define(LogLevel.Warning, new EventId(2007, nameof(logGarageSyncFailed)), "Garage synchronization failed on startup");
	private static readonly Action<ILogger, string?, string?, Exception?> logInitializing =
		LoggerMessage.Define<string?, string?>(LogLevel.Information, new EventId(2000, nameof(logInitializing)), "Initializing {AssemblyName} - version {AssemblyVersion}");
	private static readonly Action<ILogger, string?, Exception?> logEnvironment =
		LoggerMessage.Define<string?>(LogLevel.Information, new EventId(2001, nameof(logEnvironment)), "Environment: {Environment}");
	private static readonly Action<ILogger, Exception?> logAppliedMigrations =
		LoggerMessage.Define(LogLevel.Information, new EventId(2002, nameof(logAppliedMigrations)), "Applied migrations");
	private static readonly Action<ILogger, Exception?> logFailedToApplyMigrations =
		LoggerMessage.Define(LogLevel.Warning, new EventId(2003, nameof(logFailedToApplyMigrations)), "Failed to apply migrations. Continuing startup");
	private static readonly Action<ILogger, Exception?> logMigrationsDisabled =
		LoggerMessage.Define(LogLevel.Information, new EventId(2004, nameof(logMigrationsDisabled)), "Automatic migrations disabled by configuration");

	public async Task StartAsync(CancellationToken cancellationToken) {

		var assembly = Assembly.GetExecutingAssembly().GetName();
		logInitializing(logger, assembly.Name, assembly.Version?.ToString(), null);
		logEnvironment(logger, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), null);

		if (startupConfig.Value.ApplyMigrations) {
			using var scope = serviceScopeFactory.CreateScope();
			using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			try {
				await dbContext.Database.MigrateAsync(cancellationToken);
				logAppliedMigrations(logger, null);
			} catch (Exception e) {
				logFailedToApplyMigrations(logger, e);
			}
		} else {
			logMigrationsDisabled(logger, null);
		}

		using var parkingScope = serviceScopeFactory.CreateScope();
		var simulatorClientConfig = parkingScope.ServiceProvider.GetRequiredService<IOptions<SimulatorClientConfig>>();
		var garageSyncService = parkingScope.ServiceProvider.GetRequiredService<IGarageSyncService>();

		if (!simulatorClientConfig.Value.SyncGarageOnStartup) {
			logGarageSyncSkipped(logger, null);
			return;
		}

		try {
			var synced = await garageSyncService.SyncAsync(cancellationToken);
			if (synced) {
				logGarageSyncSucceeded(logger, null);
			} else {
				logGarageSyncFailed(logger, null);
				if (simulatorClientConfig.Value.FailStartupIfGarageSyncFails) {
					throw new InvalidOperationException("Garage synchronization failed on startup");
				}
			}
		} catch (Exception e) {
			logGarageSyncFailed(logger, e);
			if (simulatorClientConfig.Value.FailStartupIfGarageSyncFails) {
				throw;
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		return Task.CompletedTask;
	}
}

public class CommandService(
	IApplicationService applicationService,
	ILogger<CommandService> logger
) {
	private readonly IApplicationService applicationService = applicationService;
	private readonly ILogger<CommandService> logger = logger;
	private static readonly Action<ILogger, string, Exception?> logGeneratedApplicationToken =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(2100, nameof(logGeneratedApplicationToken)), "Generated application token. Application name {ApplicationName}");

	class GenerateAppTokenOptions {
		[Option('n', "name", Required = true, HelpText = "Application name")]
		public required string ApplicationName { get; set; }

		[Option('v', "validity-days", Required = false, HelpText = "Application token validity in days")]
		public int TokenValidityDays { get; set; } = 365;
	}

	public void GenerateApplicationToken(List<string> args) {

		CommandLine.Parser.Default.ParseArguments<GenerateAppTokenOptions>(args)
			.WithParsed(generateApplicationToken);
	}

	private void generateApplicationToken(GenerateAppTokenOptions options) {
		var expirationDate = DateTime.Now.AddDays(options.TokenValidityDays);
		var token = applicationService.GenerateToken(options.ApplicationName, expirationDate);

		Console.WriteLine($"Application token: {token}");

		logGeneratedApplicationToken(logger, options.ApplicationName, null);
	}
}
