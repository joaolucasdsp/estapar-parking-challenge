namespace EstaparParkingChallenge.Simulator.Configuration;

public class TargetApiConfig {
	public string BaseUrl { get; set; } = "https://localhost:7139";
	public string WebhookPath { get; set; } = "/api/webhook";
	public string SecretHeaderName { get; set; } = "Webhook-Signature";
	public string? Secret { get; set; }
	public bool IgnoreTlsErrors { get; set; } = true;
}

public class GarageConfig {
	public string ActiveTopology { get; set; } = "default";
	public Dictionary<string, List<GarageSectorSeed>> Topologies { get; set; } = [];
	public List<GarageSectorSeed> Sectors { get; set; } = [];
}

public class TopologySelectionRequest {
	public string Name { get; set; } = string.Empty;
}

public class GarageSectorSeed {
	public string Sector { get; set; } = string.Empty;
	public decimal BasePrice { get; set; }
	public int MaxCapacity { get; set; }
}
