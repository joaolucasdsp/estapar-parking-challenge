using EstaparParkingChallenge.Site.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Unit.Services;

[TestClass]
[TestCategory("Unit")]
public class ParkingPricingServiceTests {
	private readonly ParkingPricingService parkingPricingService = new();

	[TestMethod]
	public void GetPriceMultiplierShouldReturnExpectedByOccupancy() {
		Assert.AreEqual(0.90m, parkingPricingService.GetPriceMultiplier(0.10m));
		Assert.AreEqual(1.00m, parkingPricingService.GetPriceMultiplier(0.50m));
		Assert.AreEqual(1.10m, parkingPricingService.GetPriceMultiplier(0.75m));
		Assert.AreEqual(1.25m, parkingPricingService.GetPriceMultiplier(0.95m));
	}

	[TestMethod]
	public void CalculateAmountShouldReturnZeroForFirstThirtyMinutes() {
		var entryTime = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
		var exitTime = entryTime.AddMinutes(30);

		var amount = parkingPricingService.CalculateAmount(entryTime, exitTime, 10m, 1.00m);

		Assert.AreEqual(0m, amount);
	}

	[TestMethod]
	public void CalculateAmountShouldRoundUpHoursAndApplyMultiplier() {
		var entryTime = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
		var exitTime = entryTime.AddHours(2);

		var amount = parkingPricingService.CalculateAmount(entryTime, exitTime, 10m, 0.90m);

		Assert.AreEqual(18m, amount);
	}
}
