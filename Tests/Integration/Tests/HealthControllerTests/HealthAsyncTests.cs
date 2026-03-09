using System.Net;

using EstaparParkingChallenge.Tests.Integration.Tests.Infrastructure;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EstaparParkingChallenge.Tests.Integration.Tests.HealthControllerTests;

[TestClass]
[TestCategory("Integration")]
public class HealthAsyncTests : IntegrationApiTestBase {

	[TestMethod]
	public async Task GetHealthShouldReturnOkAsync() {
		var response = await Client.GetAsync("/api/health");

		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}
}
