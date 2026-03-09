using Microsoft.EntityFrameworkCore;

namespace EstaparParkingChallenge.Site.DataStorage;

public interface IDatabaseProvider {
	void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString);
}
