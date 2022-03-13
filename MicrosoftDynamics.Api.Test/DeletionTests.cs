namespace MicrosoftDynamics.Api.Test;

public class DeletionTests : TestBase
{
	public DeletionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
	{
	}

	//[Fact]
	//public async Task EmptyingTable_Succeeds()
	//{
	//	const string tableName = "eng_XXX";
	//	const string tableKey = "eng_XXXid";

	//	var deletionCount = 0;
	//	while (true)
	//	{
	//		// Get a number of entries
	//		var result = await Client
	//			.FindEntriesAsync(tableName)
	//			.ConfigureAwait(false);
	//		result.Should().NotBeNullOrEmpty();

	//		if (!result.Any())
	//		{
	//			break;
	//		}

	//		foreach (var entry in result)
	//		{
	//			await Client
	//				.For(tableName)
	//				.Key(entry[tableKey])
	//				.DeleteEntryAsync()
	//				.ConfigureAwait(false);
	//			deletionCount++;
	//		}

	//		Logger.LogInformation("Total deleted: {DeletionCount}", deletionCount);
	//	}
	//}
}