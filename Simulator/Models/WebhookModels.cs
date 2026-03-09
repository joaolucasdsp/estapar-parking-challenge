using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Simulator.Models;

public class WebhookEventRequest {
	[JsonPropertyName("license_plate")]
	public string LicensePlate { get; set; } = string.Empty;

	[JsonPropertyName("entry_time")]
	public DateTimeOffset? EntryTime { get; set; }

	[JsonPropertyName("exit_time")]
	public DateTimeOffset? ExitTime { get; set; }

	[JsonPropertyName("lat")]
	public decimal? Latitude { get; set; }

	[JsonPropertyName("lng")]
	public decimal? Longitude { get; set; }

	[JsonPropertyName("event_type")]
	public string EventType { get; set; } = string.Empty;
}

public class SimulateFlowRequest {
	[JsonPropertyName("license_plate")]
	public string LicensePlate { get; set; } = "SIM0001";

	[JsonPropertyName("sector")]
	public string Sector { get; set; } = "A";

	[JsonPropertyName("entry_time")]
	public DateTimeOffset? EntryTime { get; set; }

	[JsonPropertyName("exit_time")]
	public DateTimeOffset? ExitTime { get; set; }

	[JsonPropertyName("lat")]
	public decimal? Latitude { get; set; }

	[JsonPropertyName("lng")]
	public decimal? Longitude { get; set; }
}
