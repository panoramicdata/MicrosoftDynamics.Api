using Microsoft.Extensions.Configuration;
using System.IO;

namespace MicrosoftDynamics.Api.Test
{
	public class TestConfig
	{
		internal static TestConfig Load()
		{
			var builder = new ConfigurationBuilder()
				  .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../.."))
				  .AddJsonFile("appsettings.json");
			var configurationRoot = builder.Build();
			var config = new TestConfig();
			configurationRoot.Bind(config);
			return config;
		}
	}
}