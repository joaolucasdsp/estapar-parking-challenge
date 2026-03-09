using System.ComponentModel.DataAnnotations;

namespace EstaparParkingChallenge.Api.Paginated;

public class PaginatedSearchParams {
	[Range(0, 300)]
	public int Limit { get; set; }

	[Range(0, int.MaxValue)]
	public int Offset { get; set; }
}
