using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EstaparParkingChallenge.Tests.Integration;

public class AppFactory : WebApplicationFactory<Program> {
	protected override void ConfigureWebHost(IWebHostBuilder builder) {
		builder.UseEnvironment("Test");
		builder.ConfigureAppConfiguration((_, config) => {
			config.AddInMemoryCollection(new Dictionary<string, string?> {
				["Startup:ApplyMigrations"] = "false",
				["SimulatorClient:SyncGarageOnStartup"] = "false",
				["SimulatorClient:UseMockGarageOnFailure"] = "true",
				["WebhookSignature:Enabled"] = "false",
			});
		});

		builder.ConfigureTestServices(services => {
			services.AddScoped<TestDatabaseManager>();
		});
	}

	public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action) {
		using var scope = Services.CreateScope();
		await action(scope.ServiceProvider);
	}
}
