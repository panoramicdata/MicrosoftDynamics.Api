namespace MicrosoftDynamics.Api.Test;

public class IncidentTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
	[Fact]
	public async Task GetIncidents_Succeeds()
	{
		var result = await Client
			.FindEntriesAsync("incidents?$top=3")
			.ConfigureAwait(true);
		result.Should().NotBeNullOrEmpty();
	}
}
