namespace MicrosoftDynamics.Api.Test;

public sealed class TestConfig
{
	public MicrosoftDynamicsClientOptions Options { get; set; } = new();

	internal static TestConfig Load()
	{
		var builder = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
			.AddUserSecrets<TestConfig>(optional: true);

		var configurationRoot = builder.Build();
		var config = new TestConfig();

		// Try binding with Options wrapper first (appsettings.json format)
		configurationRoot.Bind(config);

		// If Options wasn't populated, try binding directly to Options (user secrets format)
		if (string.IsNullOrEmpty(config.Options.Uri?.ToString()))
		{
			configurationRoot.Bind(config.Options);
		}

		if (config.Options.AccessToken?.Length == 0)
		{
			config.Options.AccessToken = null;
		}

		return config;
	}
}
