using EstaparParkingChallenge.Api;

namespace EstaparParkingChallenge.Site.Classes;

public class ApiException(
	ErrorCodes errorCode,
	string errorMessage,
	object? details = null,
	Exception? innerException = null
) : Exception(formatMessage(errorCode, errorMessage, details), innerException) {

	public ErrorModel Error { get; set; } = new ErrorModel {
		CodeStr = errorCode.ToString(),
		Details = details != null ? getDetailsDictionary(details) : null,
		Message = errorMessage,
	};

	private static string formatMessage(ErrorCodes errorCode, string errorMessage, object? details) {
		var text = $"API error {errorCode}: {errorMessage}";
		if (details == null) {
			return text;
		}

		var detailsKeyPair = getDetailsDictionary(details).Select(x => $"{x.Key}: {x.Value}");
		return $"{text} {string.Join(", ", detailsKeyPair)}";
	}

	private static Dictionary<string, string> getDetailsDictionary(object details) {
		var dic = new Dictionary<string, string>();
		foreach (var descriptor in details.GetType().GetProperties()) {
			dic[descriptor.Name] = descriptor.GetValue(details, null)?.ToString() ?? "null";
		}

		return dic;
	}
}
