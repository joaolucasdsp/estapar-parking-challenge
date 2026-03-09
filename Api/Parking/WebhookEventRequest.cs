using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Api.Parking;

public class WebhookEventRequest {
	[Required]
	[JsonPropertyName("license_plate")]
	public required string LicensePlate { get; set; }

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
	public required string EventType { get; set; }
}
