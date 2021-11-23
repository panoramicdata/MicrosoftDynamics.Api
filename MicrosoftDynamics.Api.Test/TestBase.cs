namespace MicrosoftDynamics.Api.Test;

public class TestBase
{
	protected TestConfig TestConfig { get; }
	protected ILogger Logger { get; }

	public TestBase(ITestOutputHelper testOutputHelper)
	{
		Logger = testOutputHelper.BuildLoggerFor<TestBase>();
		TestConfig = TestConfig.Load();
		TestConfig.Options.Logger = testOutputHelper.BuildLoggerFor<MicrosoftDynamicsClient>();
	}

	protected MicrosoftDynamicsClient Client => new(TestConfig.Options);
}
