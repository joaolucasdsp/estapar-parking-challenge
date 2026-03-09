using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Api.Parking;

public class RevenueResponse {
	[JsonPropertyName("amount")]
	public decimal Amount { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = "BRL";

	[JsonPropertyName("timestamp")]
	public DateTimeOffset Timestamp { get; set; }
}
