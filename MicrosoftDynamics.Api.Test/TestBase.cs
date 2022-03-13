namespace MicrosoftDynamics.Api.Test;

public class TestBase
{
	private MicrosoftDynamicsClient? _client;

	protected TestConfig TestConfig { get; }
	protected ILogger Logger { get; }

	public TestBase(ITestOutputHelper testOutputHelper)
	{
		Logger = testOutputHelper.BuildLoggerFor<TestBase>();
		TestConfig = TestConfig.Load();
		TestConfig.Options.Logger = testOutputHelper.BuildLoggerFor<MicrosoftDynamicsClient>();
		TestConfig.Options.LogMetadata = false;
	}

	protected MicrosoftDynamicsClient Client
		=> _client ??= new(TestConfig.Options);
}
