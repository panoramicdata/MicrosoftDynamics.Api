using Simple.OData.Client;

namespace MicrosoftDynamics.Api
{
	public class MicrosoftDynamicsClient : ODataClient
	{
		public MicrosoftDynamicsClient(ODataClientSettings settings) : base(Update(settings))
		{
		}

		private static ODataClientSettings Update(ODataClientSettings settings)
			=> settings;
	}
}
