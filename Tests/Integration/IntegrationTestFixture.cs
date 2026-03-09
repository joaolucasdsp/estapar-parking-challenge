namespace EstaparParkingChallenge.Tests.Integration;

public static class IntegrationTestFixture {
	private static readonly Lazy<AppFactory> appFactory = new(() => new AppFactory());
	private static readonly Lazy<HttpClient> client = new(() => appFactory.Value.CreateClient());

	public static AppFactory AppFactory => appFactory.Value;
	public static HttpClient Client => client.Value;

	public static void Dispose() {
		if (client.IsValueCreated) {
			client.Value.Dispose();
		}

		if (appFactory.IsValueCreated) {
			appFactory.Value.Dispose();
		}
	}
}
