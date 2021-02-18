using Microsoft.Extensions.Logging;
using Simple.OData.Client;
using System;
using Xunit.Abstractions;

namespace MicrosoftDynamics.Api.Test
{
	public class TestBase
	{
		protected TestConfig TestConfig { get; }
		protected ILogger Logger { get; }

		public TestBase(ITestOutputHelper testOutputHelper)
		{
			Logger = testOutputHelper.BuildLoggerFor<TestBase>();

			TestConfig = TestConfig.Load();
		}

		protected MicrosoftDynamicsClient GetDynamicsIms()
		{
			//var microsoftDynamicsClient = new MicrosoftDynamicsClient(new ODataClientSettings
			//{
			//	Credentials = new ClientSecrets
			//	{
			//		Id = TestConfig.ClientId,
			//		Secret = TestConfig.ClientSecret,
			//	}
			//});

			//return microsoftDynamicsClient;
			throw new NotImplementedException();
		}
	}
}