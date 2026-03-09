namespace EstaparParkingChallenge.Site.Configuration;

public static class AppConfiguration {

	public const string JwtSection = "Jwt";
	public const string RedisSection = "Redis";
	public const string StartupSection = "Startup";
	public const string SimulatorClientSection = "SimulatorClient";
	public const string WebhookSignatureSection = "WebhookSignature";

	public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration) {

		services.Configure<JwtConfig>(configuration.GetSection(JwtSection));
		services.Configure<RedisConfig>(configuration.GetSection(RedisSection));
		services.Configure<StartupConfig>(configuration.GetSection(StartupSection));
		services.Configure<SimulatorClientConfig>(configuration.GetSection(SimulatorClientSection));
		services.Configure<WebhookSignatureConfig>(configuration.GetSection(WebhookSignatureSection));

		return services;
	}
}
