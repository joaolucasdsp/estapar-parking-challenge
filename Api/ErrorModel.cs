using System.Text.Json.Serialization;

namespace EstaparParkingChallenge.Api;

public class ErrorModel {
	[JsonPropertyName("code")]
	public string CodeStr { get; set; } = string.Empty;

	[JsonIgnore]
	public ErrorCodes Code {
		get {
			try {
				return (ErrorCodes)Enum.Parse(typeof(ErrorCodes), CodeStr);
			} catch {
				return ErrorCodes.Unknown;
			}
		}
		set {
			CodeStr = value.ToString();
		}
	}
	public string Message { get; set; } = string.Empty;
	public Dictionary<string, string>? Details { get; set; }
	public override string ToString() {
		var detailsString = string.Empty;
		if (Details != null && Details.Count > 0) {
			detailsString = Environment.NewLine + string.Join(Environment.NewLine, Details);
		}
		return $"{CodeStr} - {Message}{detailsString}";
	}
}
