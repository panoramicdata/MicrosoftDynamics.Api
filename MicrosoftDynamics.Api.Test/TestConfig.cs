namespace MicrosoftDynamics.Api.Test;

public class TestConfig
{
	public MicrosoftDynamicsClientOptions Options { get; set; } = new();

	internal static TestConfig Load()
	{
		var builder = new ConfigurationBuilder()
			  .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../.."))
			  .AddJsonFile("appsettings.json");
		var configurationRoot = builder.Build();
		var config = new TestConfig();
		configurationRoot.Bind(config);
		if (config.Options.AccessToken?.Length == 0)
		{
			config.Options.AccessToken = null;
		}
		return config;
	}
}
