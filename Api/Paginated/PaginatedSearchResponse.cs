namespace EstaparParkingChallenge.Api.Paginated;

public class PaginatedSearchResponse<T>(IEnumerable<T> items, int totalCount) {
	public IEnumerable<T> Items { get; set; } = items;

	public int TotalCount { get; set; } = totalCount;
}
