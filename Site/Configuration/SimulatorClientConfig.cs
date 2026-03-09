namespace EstaparParkingChallenge.Site.Configuration;

public class SimulatorClientConfig {
	public string? GarageBaseUrl { get; set; }
	public string GarageEndpoint { get; set; } = "/garage";
	public int GarageConfigCacheMinutes { get; set; } = 5;
	public bool SyncGarageOnStartup { get; set; } = true;
	public bool FailStartupIfGarageSyncFails { get; set; }
	public bool UseMockGarageOnFailure { get; set; } = true;
}

public class WebhookSignatureConfig {
	public bool Enabled { get; set; }
	public string HeaderName { get; set; } = "Webhook-Signature";
	public string? Secret { get; set; } = "whsec_9f8b3c7a4d1e2f6a8b9c0d3e5f7a1b2c4d6e8f9a0b1c3d5e7f9a2b4c6d8e0f1";
	public int ToleranceSeconds { get; set; } = 300;
}
