using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using EstaparParkingChallenge.Tests.Integration.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Integration.Tests.ParkingControllerTests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class RevenueAsyncTests : IntegrationDatabaseTestBase {

	[TestMethod]
	public async Task ShouldReturnRevenueFromCompletedFlowAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.SeedGarageAsync(capacity: 4, basePrice: 10m);
		});

		var entryResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0001",
			entry_time = "2025-01-01T10:00:00.000Z",
			event_type = "ENTRY",
		});
		Assert.AreEqual(HttpStatusCode.OK, entryResponse.StatusCode);

		var parkedResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0001",
			lat = -23.561684,
			lng = -46.655981,
			event_type = "PARKED",
		});
		Assert.AreEqual(HttpStatusCode.OK, parkedResponse.StatusCode);

		var exitResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "ZUL0001",
			exit_time = "2025-01-01T12:00:00.000Z",
			event_type = "EXIT",
		});
		Assert.AreEqual(HttpStatusCode.OK, exitResponse.StatusCode);

		var revenueResponse = await Client.GetAsync("/api/revenue?date=2025-01-01&sector=A");
		Assert.AreEqual(HttpStatusCode.OK, revenueResponse.StatusCode);

		using var responseStream = await revenueResponse.Content.ReadAsStreamAsync();
		var json = await JsonDocument.ParseAsync(responseStream);
		var amount = json.RootElement.GetProperty("amount").GetDecimal();
		Assert.AreEqual(18.0m, amount);
	}

	[TestMethod]
	public async Task ShouldReturnZeroRevenueWhenNoCompletedSessionAsync() {
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

		var revenueResponse = await Client.GetAsync("/api/revenue?date=2025-01-01&sector=A");
		Assert.AreEqual(HttpStatusCode.OK, revenueResponse.StatusCode);

		using var responseStream = await revenueResponse.Content.ReadAsStreamAsync();
		var json = await JsonDocument.ParseAsync(responseStream);
		var amount = json.RootElement.GetProperty("amount").GetDecimal();
		Assert.AreEqual(0m, amount);
	}

	[TestMethod]
	public async Task ShouldNotChargeWhenVehicleDidNotParkAsync() {
		await ExecuteScopeAsync(async serviceProvider => {
			var databaseManager = serviceProvider.GetRequiredService<TestDatabaseManager>();
			await databaseManager.SeedGarageAsync(capacity: 10, basePrice: 12m);
		});

		var entryResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "NPK0001",
			entry_time = "2025-01-01T10:00:00.000Z",
			event_type = "ENTRY",
		});
		Assert.AreEqual(HttpStatusCode.OK, entryResponse.StatusCode);

		var exitResponse = await Client.PostAsJsonAsync("/api/webhook", new {
			license_plate = "NPK0001",
			exit_time = "2025-01-01T12:00:00.000Z",
			event_type = "EXIT",
		});
		Assert.AreEqual(HttpStatusCode.OK, exitResponse.StatusCode);

		var revenueResponse = await Client.GetAsync("/api/revenue?date=2025-01-01&sector=A");
		Assert.AreEqual(HttpStatusCode.OK, revenueResponse.StatusCode);

		using var responseStream = await revenueResponse.Content.ReadAsStreamAsync();
		var json = await JsonDocument.ParseAsync(responseStream);
		var amount = json.RootElement.GetProperty("amount").GetDecimal();
		Assert.AreEqual(0m, amount);
	}
}
