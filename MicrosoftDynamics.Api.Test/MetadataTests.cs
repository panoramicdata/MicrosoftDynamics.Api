namespace MicrosoftDynamics.Api.Test;

public class MetadataTests(ITestOutputHelper output) : TestBase(output)
{
	[Fact]
	public async Task GetMetadata_Succeeds()
	{
		var result = await Client.ODataClient
			.GetMetadataAsync(null, CancellationToken)
			.ConfigureAwait(true);

		result.Should().NotBeNull();
		result.EntitySets.Should().NotBeEmpty();
	}
}
