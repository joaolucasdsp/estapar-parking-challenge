using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Api;

public class ErrorModel {
	[JsonPropertyName("code")]
	public required string CodeStr { get; set; }

	[JsonIgnore]
	public ErrorCodes Code {
		get {
			try {
				return Enum.Parse<ErrorCodes>(CodeStr);
			} catch {
				return ErrorCodes.Unknown;
			}
		}
		set {
			CodeStr = value.ToString();
		}
	}
	public required string Message { get; set; }
	public Dictionary<string, string>? Details { get; set; }
	public override string ToString() {
		var detailsString = string.Empty;
		if (Details != null && Details.Count > 0) {
			detailsString = Environment.NewLine + string.Join(Environment.NewLine, Details);
		}
		return $"{CodeStr} - {Message}{detailsString}";
	}
}
