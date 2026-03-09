namespace EstaparParkingChallenge.Site.Services;

public interface IParkingPricingService {
	decimal GetPriceMultiplier(decimal occupancyRate);
	decimal CalculateAmount(DateTimeOffset entryTime, DateTimeOffset? exitTime, decimal basePriceAtEntry, decimal entryPriceMultiplier);
}

public class ParkingPricingService : IParkingPricingService {
	public decimal GetPriceMultiplier(decimal occupancyRate) {
		if (occupancyRate < 0.25m) {
			return 0.90m;
		}

		if (occupancyRate <= 0.50m) {
			return 1.00m;
		}

		if (occupancyRate <= 0.75m) {
			return 1.10m;
		}

		return 1.25m;
	}

	public decimal CalculateAmount(DateTimeOffset entryTime, DateTimeOffset? exitTime, decimal basePriceAtEntry, decimal entryPriceMultiplier) {
		if (!exitTime.HasValue) {
			return 0m;
		}

		var duration = exitTime.Value - entryTime;
		if (duration <= TimeSpan.FromMinutes(30)) {
			return 0m;
		}

		var billableHours = (int)Math.Ceiling(duration.TotalHours);
		var fullPrice = basePriceAtEntry * billableHours;
		var adjustedPrice = fullPrice * entryPriceMultiplier;
		return decimal.Round(adjustedPrice, 2, MidpointRounding.AwayFromZero);
	}
}
