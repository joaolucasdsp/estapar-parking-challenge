using System.Net;
using System.Net.Http.Json;

using EstaparParkingChallenge.Tests.Integration.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Integration.Tests.ParkingControllerTests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class WebhookAsyncTests : IntegrationDatabaseTestBase {

	[TestMethod]
	public async Task EntryShouldBeNoOpWhenFullAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.SeedGarageAsync(capacity: 1, basePrice: 10m);
		});

		var firstEntry = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0001",
			entry_time = "2025-01-01T10:00:00.000Z",
			event_type = "ENTRY",
		});
		Assert.AreEqual(HttpStatusCode.OK, firstEntry.StatusCode);

		var secondEntry = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0002",
			entry_time = "2025-01-01T10:05:00.000Z",
			event_type = "ENTRY",
		});
		Assert.AreEqual(HttpStatusCode.OK, secondEntry.StatusCode);

		var secondExit = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0002",
			exit_time = "2025-01-01T11:00:00.000Z",
			event_type = "EXIT",
		});
		Assert.AreEqual(HttpStatusCode.OK, secondExit.StatusCode);
	}

	[TestMethod]
	public async Task OutOfOrderExitShouldReturnOkAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.SeedGarageAsync(capacity: 10, basePrice: 15m);
		});

		var exitResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "UNK0001",
			exit_time = "2025-01-01T11:00:00.000Z",
			event_type = "EXIT",
		});
		Assert.AreEqual(HttpStatusCode.OK, exitResponse.StatusCode);
	}

	[TestMethod]
	public async Task WebhookAliasShouldAcceptEventsAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.SeedGarageAsync(capacity: 10, basePrice: 15m);
		});

		var entryResponse = await Client.PostAsJsonAsync("/webhook", new {
			license_plate = "ALS0001",
			entry_time = "2025-01-01T10:00:00.000Z",
			event_type = "ENTRY",
		});

		Assert.AreEqual(HttpStatusCode.OK, entryResponse.StatusCode);
	}
}
