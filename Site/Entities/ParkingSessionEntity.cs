using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.Entities;

public class ParkingSessionEntity {

	[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	[MaxLength(20)]
	public required string LicensePlate { get; set; }

	[MaxLength(20)]
	public int? GarageSectorId { get; set; }
	public GarageSectorEntity? GarageSector { get; set; }

	public DateTimeOffset EntryTime { get; set; }

	public DateTimeOffset? ExitTime { get; set; }

	[Precision(10, 4)]
	public decimal EntryPriceMultiplier { get; set; }

	[Precision(18, 2)]
	public decimal? BasePriceAtEntry { get; set; }

	[Precision(18, 2)]
	public decimal? AmountCharged { get; set; }

	public int? SpotId { get; set; }

	public bool IsParked { get; set; }

	public bool IsActive => ExitTime == null;
}
