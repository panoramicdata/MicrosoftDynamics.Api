using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftDynamics.Api.Test
{
	public class IncidentTests : TestBase
	{
		public IncidentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
		{
		}

		[Fact]
		public async Task GetIncidents_Succeeds()
		{
			var result = await Client
				.FindEntriesAsync("incidents?$top=3")
				.ConfigureAwait(false);
			result.Should().NotBeNullOrEmpty();
		}
	}
}
