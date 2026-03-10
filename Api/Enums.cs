using EstaparParkingChallenge.Api.DataAnnotations;

namespace EstaparParkingChallenge.Api {

	public enum ErrorCodes {
		Unknown,
		UnsupportedParkingEventType,
		InvalidParkingEventType,
	}

	public enum ParkingEventType {
		[Code("E")]
		Entry = 1,

		[Code("P")]
		Parked = 2,

		[Code("X")]
		Exit = 3,
	}
}
