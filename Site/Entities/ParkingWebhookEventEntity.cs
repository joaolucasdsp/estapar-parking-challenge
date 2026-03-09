using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using EstaparParkingChallenge.Api;
using EstaparParkingChallenge.Site.Classes.Utils;

namespace EstaparParkingChallenge.Site.Entities;

public class ParkingWebhookEventEntity {

	[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	[MaxLength(120)]
	public required string IdempotencyKey { get; set; }

	[MaxLength(20)]
	public required string LicensePlate { get; set; }

	#region ParkingEventType
	public ParkingEventType EventType { get; set; }

	[Required, MaxLength(3)]
	public string EventTypeCode {
		get => EnumEncoding.GetCode(EventType);
		set => EventType = EnumEncoding.GetValue<ParkingEventType>(value);
	}
	#endregion

	public DateTimeOffset OccurredAt { get; set; }

	public DateTimeOffset ProcessedAt { get; set; }
}
