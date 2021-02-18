using Newtonsoft.Json;
using Simple.OData.Client;
using System;
using System.Net.Http;
using System.Text;
using System.Web;

namespace MicrosoftDynamics.Api
{
	public class MicrosoftDynamicsClient : ODataClient
	{
		public MicrosoftDynamicsClient(MicrosoftDynamicsClientOptions options) : base(GetSettings(options))
		{
		}

		private static ODataClientSettings GetSettings(MicrosoftDynamicsClientOptions options)
		{
			// Validate the options
			options.Validate();
			// The options are valid.

			var settings = new ODataClientSettings(new Uri(options.Url + $"api/data/v{options.OdataApiVersion}/"));
			settings.BeforeRequestAsync += async (HttpRequestMessage message) =>
			{
				if (options.AccessToken is null)
				{
					using var authHttpClient = new HttpClient
					{
						BaseAddress = new(options.AuthenticationUrl)
					};
					authHttpClient.DefaultRequestHeaders.Authorization = new("Basic", Base64Encode($"{options.ClientId}:{options.ClientSecret}"));
					var scope = HttpUtility.UrlEncode($"{options.Url!.TrimEnd('/')}/.default");
					var request = new HttpRequestMessage(HttpMethod.Post, "")
					{
						Content = new StringContent(
							$"grant_type=client_credentials&scope={scope}",
							Encoding.UTF8,
							"application/x-www-form-urlencoded")
					};
					var response = await authHttpClient
						.SendAsync(request)
						.ConfigureAwait(false);
					var responseText = await response
						.Content
						.ReadAsStringAsync()
						.ConfigureAwait(false);
					options.AccessToken = GetBearerToken(responseText);
				}
				message.Headers.Add("Authorization", "Bearer " + options.AccessToken);
			};
			return settings;
		}

		private static string GetBearerToken(string responseText)
		{
			var bearerTokenResponse = JsonConvert.DeserializeObject<BearerTokenResponse>(responseText);
			return bearerTokenResponse!.AccessToken;
		}

		public static string Base64Encode(string plainText)
		{
			var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			return Convert.ToBase64String(plainTextBytes);
		}
	}
}
