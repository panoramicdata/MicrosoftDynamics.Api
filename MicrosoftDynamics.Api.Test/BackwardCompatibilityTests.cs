namespace MicrosoftDynamics.Api.Test;

public class BackwardCompatibilityTests(ITestOutputHelper output) : TestBase(output)
{
	[Fact]
	public async Task FindEntriesAsync_WithRawQuery_Succeeds()
	{
		var results = await Client
			.FindEntriesAsync("incidents?$top=3", CancellationToken)
			.ConfigureAwait(true);

		results.Should().NotBeNull();
		results.Should().HaveCountLessThanOrEqualTo(3);
	}

	[Fact]
	public async Task For_NonGeneric_WithTop_GetAsync_Succeeds()
	{
		// Uses the new API pattern: client.GetAsync(query, ct)
		var response = await Client.ODataClient
			.GetAsync(Client.For("incidents").Top(2), CancellationToken)
			.ConfigureAwait(true);

		response.Should().NotBeNull();
		response.Value.Should().HaveCountLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task For_NonGeneric_GetAllAsync_Succeeds()
	{
		// Uses the new API pattern: client.GetAllAsync(query, ct)
		var response = await Client.ODataClient
			.GetAllAsync(Client.For("incidents").Top(5), CancellationToken)
			.ConfigureAwait(true);

		response.Should().NotBeNull();
		response.Value.Should().HaveCountLessThanOrEqualTo(5);
	}
}
