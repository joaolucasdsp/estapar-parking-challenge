namespace EstaparParkingChallenge.Site.Configuration;

public class RedisConfig {
	public bool Enabled { get; set; } = true;
	public string? ConnectionString { get; set; }
	public string? Endpoint { get; set; }
}
