using EstaparParkingChallenge.Site.Classes;

namespace EstaparParkingChallenge.Site.DataStorage;

public static class DatabaseProviderFactory {
	public static IDatabaseProvider Create(DatabaseProvider provider) {
		return provider switch {
			DatabaseProvider.PostgreSql => new PostgresDatabaseProvider(),
			DatabaseProvider.SqlServer => new SqlServerDatabaseProvider(),
			_ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
		};
	}

	public static string GetConnectionStringKey(DatabaseProvider provider) {
		return provider switch {
			DatabaseProvider.PostgreSql => "PostgreSqlConnection",
			DatabaseProvider.SqlServer => "SqlServerConnection",
			_ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
		};
	}
}
