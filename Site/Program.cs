using System.Text;
using System.Text.Json.Serialization;

using EstaparParkingChallenge.Site.Configuration;
using EstaparParkingChallenge.Site.DataStorage;
using EstaparParkingChallenge.Site.Filters;
using EstaparParkingChallenge.Site.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSerilog(new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration, "Serilog")
	.CreateLogger()
);

// Add services to the container.

builder.Services.AddControllers(o => {
	o.Filters.Add<HandleExceptionFilter>();
}).AddJsonOptions(options => {
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Database configuration
builder.Services.AddDatabaseProvider(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// OpenAPI + Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1");

builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddAppConfiguration(builder.Configuration);

// Authentication - JWT
var jwtConfig = builder.Configuration.GetSection(AppConfiguration.JwtSection).Get<JwtConfig>();
if (jwtConfig is null || string.IsNullOrWhiteSpace(jwtConfig.Secret) || string.IsNullOrWhiteSpace(jwtConfig.Issuer)) {
	throw new InvalidOperationException("Invalid JWT configuration");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
	options.TokenValidationParameters = new TokenValidationParameters {
		ValidateAudience = false,
		ValidateIssuer = true,
		ValidIssuer = jwtConfig.Issuer,
		ValidateIssuerSigningKey = true,
		ValidateLifetime = true,
		ClockSkew = TimeSpan.FromMinutes(1),
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtConfig.Secret)),
	};
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var commandArgs = args.ToList();
if (commandArgs.Count != 0 && args[0] == "--gen-app-token") {
	using var scope = app.Services.CreateScope();
	var commandService = scope.ServiceProvider.GetRequiredService<CommandService>();
	commandArgs.RemoveAt(0);
	commandService.GenerateApplicationToken(commandArgs);
} else {
	app.MapOpenApi("/openapi/{documentName}.json");
	app.MapScalarApiReference(options => {
		options.Title = "AspNet Boilerplate API";
	});

	app.UseAuthentication();
	app.UseAuthorization();

	app.MapControllers().RequireAuthorization();

	app.Run();
}

