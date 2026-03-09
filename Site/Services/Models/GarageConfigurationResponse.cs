using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Site.Services.Models;

public class GarageConfigurationResponse {
	[JsonPropertyName("garage")]
	public List<GarageSectorResponse> Garage { get; set; } = [];

	[JsonPropertyName("spots")]
	public List<GarageSpotResponse> Spots { get; set; } = [];
}

public class GarageSectorResponse {
	[JsonPropertyName("sector")]
	public required string Sector { get; set; }

	[JsonPropertyName("basePrice")]
	public decimal BasePrice { get; set; }

	[JsonPropertyName("max_capacity")]
	public int MaxCapacity { get; set; }
}

public class GarageSpotResponse {
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("sector")]
	public required string Sector { get; set; }

	[JsonPropertyName("lat")]
	public decimal Latitude { get; set; }

	[JsonPropertyName("lng")]
	public decimal Longitude { get; set; }
}
