using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EstaparParkingChallenge.Api;
using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Configuration;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EstaparParkingChallenge.Site.Services;

public interface IApplicationService {
	Application GetApplication();

	string GenerateToken(string applicationName, DateTime? expirationDate = null);
}

public class ApplicationService(
	IHttpContextAccessor httpContextAccessor,
	IOptions<JwtConfig> jwtConfig
) : IApplicationService {

	private Application? application;

	private const string CreationDateClaim = "urn:aspnet-boilerplate:claims:created-at";

	public Application GetApplication() {
		if (application != null) {
			return application;
		}

		var user = httpContextAccessor.HttpContext?.User;
		if (user == null || !user.Claims.Any()) {
			throw new InvalidOperationException("Application not received");
		}

		var role = user.FindFirstValue(ClaimTypes.Role);
		if (role != ApiConstants.ApplicationRole) {
			throw new UnauthorizedAccessException("User is not an application");
		}

		var name = user.FindFirstValue(ClaimTypes.Name);
		if (string.IsNullOrWhiteSpace(name)) {
			throw new InvalidOperationException("Application name claim is missing");
		}

		var creationDateRaw = user.FindFirstValue(CreationDateClaim);
		if (!long.TryParse(creationDateRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var creationDateUnixTimeSeconds)) {
			throw new InvalidOperationException("Application creation date claim is missing or invalid");
		}

		application = new Application {
			Name = name,
			CreationDate = DateTimeOffset.FromUnixTimeSeconds(creationDateUnixTimeSeconds)
		};

		return application;
	}

	public string GenerateToken(string applicationName, DateTime? expirationDate = null) {
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(jwtConfig.Value.Secret);
		var tokenDescriptor = new SecurityTokenDescriptor {
			Issuer = jwtConfig.Value.Issuer,
			Subject = new ClaimsIdentity(new Claim[]
			{
				new(ClaimTypes.Name, applicationName),
				new(ClaimTypes.Role, ApiConstants.ApplicationRole),
				new(CreationDateClaim, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
			}),
			Expires = expirationDate ?? DateTime.UtcNow.AddYears(10),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};
		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}
}
