using Microsoft.OData.Edm;

namespace MicrosoftDynamics.Api.Test;

public class MetadataTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
	[Fact]
	public async Task GetMetadata_Succeeds()
	{
		var result = await Client
			.GetMetadataAsync(default)
			.ConfigureAwait(true);

		var model = result as IEdmModel;

		model.Should().NotBeNull();
	}
}
