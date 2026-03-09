namespace EstaparParkingChallenge.Simulator.Configuration;

public class TargetApiConfig {
	public string BaseUrl { get; set; } = "https://localhost:7139";
	public string WebhookPath { get; set; } = "/webhook";
	public string SecretHeaderName { get; set; } = "Webhook-Signature";
	public string? Secret { get; set; }
}

public class GarageConfig {
	public List<GarageSectorSeed> Sectors { get; set; } = [
		new() { Sector = "A", BasePrice = 10m, MaxCapacity = 40 },
		new() { Sector = "B", BasePrice = 12m, MaxCapacity = 35 },
		new() { Sector = "C", BasePrice = 15m, MaxCapacity = 25 },
	];
}

public class GarageSectorSeed {
	public string Sector { get; set; } = string.Empty;
	public decimal BasePrice { get; set; }
	public int MaxCapacity { get; set; }
}
