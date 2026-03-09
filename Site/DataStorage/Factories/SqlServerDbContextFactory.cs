using EstaparParkingChallenge.Site.Classes;
using EstaparParkingChallenge.Site.Entities;

using Microsoft.EntityFrameworkCore.Design;

namespace EstaparParkingChallenge.Site.DataStorage.Factories;

public sealed class SqlServerDbContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext> {
	public SqlServerDbContext CreateDbContext(string[] args) {
		var options = DesignTimeDbContextFactoryHelper.CreateOptions<SqlServerDbContext>(DatabaseProvider.SqlServer);
		return new SqlServerDbContext(options);
	}
}
