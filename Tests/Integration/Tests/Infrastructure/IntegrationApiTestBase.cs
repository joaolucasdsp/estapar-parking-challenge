using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Integration.Tests.Infrastructure;

public abstract class IntegrationApiTestBase {
	protected static HttpClient Client => IntegrationTestFixture.Client;

	protected static Task ExecuteScopeAsync(Func<IServiceProvider, Task> action) {
		return IntegrationTestFixture.AppFactory.ExecuteScopeAsync(action);
	}
}

public abstract class IntegrationDatabaseTestBase : IntegrationApiTestBase {
	[TestInitialize]
	public async Task ResetDatabaseAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.ResetAsync();
		});
	}
}
