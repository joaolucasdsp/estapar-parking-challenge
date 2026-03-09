using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore.Design;

namespace EstaparParkingChallenge.Site.DataStorage.Factories;

public sealed class PostgreSqlDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext> {
	public PostgresDbContext CreateDbContext(string[] args) {
		var options = DesignTimeDbContextFactoryHelper.CreateOptions<PostgresDbContext>(DatabaseProvider.PostgreSql);
		return new PostgresDbContext(options);
	}
}
