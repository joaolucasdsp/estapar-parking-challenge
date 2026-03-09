using EstaparParkingChallenge.Site.Classes;

namespace EstaparParkingChallenge.Site.Configuration;

public class StartupConfig {
	public bool ApplyMigrations { get; set; } = true;
	public DatabaseProvider Database { get; set; } = DatabaseProvider.PostgreSql;
}
