using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Entities;

public class GarageSectorEntity {

	public int Id { get; set; }

	[MaxLength(20)]
	public required string Sector { get; set; }

	[Precision(18, 2)]
	public decimal BasePrice { get; set; }

	public int MaxCapacity { get; set; }

	public List<GarageSpotEntity> Spots { get; set; } = [];
}
