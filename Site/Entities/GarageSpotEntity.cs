using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Entities;

public class GarageSpotEntity {

	public int Id { get; set; }

	public int GarageSectorId { get; set; }
	public GarageSectorEntity GarageSector { get; set; } = null!;

	[Precision(9, 6)]
	public decimal Latitude { get; set; }

	[Precision(9, 6)]
	public decimal Longitude { get; set; }

	public bool IsOccupied { get; set; }

	[MaxLength(20)]
	public string? OccupiedByLicensePlate { get; set; }
}
