using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Api.Parking;

public class WebhookEventRequest {
	[Required]
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

	[Required]
	[JsonPropertyName("event_type")]
	public string EventType { get; set; } = string.Empty;
}
