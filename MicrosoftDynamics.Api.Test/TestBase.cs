namespace MicrosoftDynamics.Api.Test;

// Every test deriving from this base calls a live Dynamics 365 instance with credentials from user
// secrets. CI has none, so the coverage job excludes them with --filter "Category!=Integration".
[Trait("Category", "Integration")]
public abstract class TestBase(ITestOutputHelper output) : IDisposable
{
	private MicrosoftDynamicsClient? _client;
	private bool _disposed;

	protected ITestOutputHelper Output { get; } = output;
	protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	protected TestConfig TestConfig { get; } = TestConfig.Load();

	protected ILogger Logger => new XunitLogger(Output, "MicrosoftDynamics.Api.Test", LogLevel.Debug);

	protected MicrosoftDynamicsClient Client
	{
		get
		{
			if (_client is null)
			{
				TestConfig.Options.Logger = new XunitLogger(Output, "MicrosoftDynamics.Api", LogLevel.Debug);
				TestConfig.Options.LogMetadata = false;
				_client = new MicrosoftDynamicsClient(TestConfig.Options);
			}

			return _client;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_client?.Dispose();
			}

			_disposed = true;
		}
	}
}
