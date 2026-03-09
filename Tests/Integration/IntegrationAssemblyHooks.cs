using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Integration;

[TestClass]
public class IntegrationAssemblyHooks {
	[AssemblyCleanup]
	public static void AssemblyCleanup() {
		IntegrationTestFixture.Dispose();
	}
}
