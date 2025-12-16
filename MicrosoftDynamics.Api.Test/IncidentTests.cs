namespace MicrosoftDynamics.Api.Test;

public class IncidentTests(ITestOutputHelper output) : TestBase(output)
{
	[Fact]
	public async Task GetIncidents_Succeeds()
	{
		var query = Client.For<Dictionary<string, object>>("incidents").Top(3);
		var result = await Client
			.GetAsync(query, CancellationToken)
			.ConfigureAwait(true);
		result.Value.Should().NotBeNullOrEmpty();
	}
}
