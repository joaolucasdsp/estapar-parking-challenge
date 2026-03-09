namespace EstaparParkingChallenge.Site.Configuration;

public class JwtConfig {
	public required string Secret { get; set; }
	public required string Issuer { get; set; }
}
