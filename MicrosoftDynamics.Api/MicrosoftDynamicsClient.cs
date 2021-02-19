﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MicrosoftDynamics.Api
{
	public class MicrosoftDynamicsClient : ODataClient
	{
		private static Uri? _uri;
		private string? _accessToken;

		public MicrosoftDynamicsClient(MicrosoftDynamicsClientOptions options) : base(GetSettings(options))
		{
			_accessToken = options.AccessToken;
		}

		/// <summary>
		/// This permits updates using @odata.bind.   You will have to add a parameter for the namespace, like:
		/// - "@odata.type": "#Microsoft.Dynamics.CRM.incident"
		/// </summary>
		/// <param name="path"></param>
		/// <param name="entity"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>The resulting body, interpreted as a JObject</returns>
		public async Task<JObject> PostAsync(string path, object entity, CancellationToken cancellationToken)
		{
			using var httpClient = new HttpClient
			{
				BaseAddress = _uri!
			};
			var request = new HttpRequestMessage(HttpMethod.Post, path)
			{
				Content = new StringContent(JsonConvert.SerializeObject(entity),
					Encoding.UTF8,
					"application/json")
			};
			request.Headers.Add("Authorization", "Bearer " + _accessToken);
			var httpResponseMessage = await httpClient
				.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
			var responseBody = await httpResponseMessage
				.Content
				.ReadAsStringAsync()
				.ConfigureAwait(false);
			return JsonConvert.DeserializeObject<JObject>(responseBody);
		}

		private static ODataClientSettings GetSettings(MicrosoftDynamicsClientOptions options)
		{
			// Validate the options
			options.Validate();
			// The options are valid.

			_uri = new Uri(options.Url + $"api/data/v{options.OdataApiVersion}/");
			var settings = new ODataClientSettings(_uri);
			settings.BeforeRequestAsync += async (HttpRequestMessage request) =>
			{
				if (options.AccessToken is null)
				{
					using var authHttpClient = new HttpClient
					{
						BaseAddress = new(options.AuthenticationUrl)
					};
					authHttpClient.DefaultRequestHeaders.Authorization = new("Basic", Base64Encode($"{options.ClientId}:{options.ClientSecret}"));
					var scope = HttpUtility.UrlEncode($"{options.Url!.TrimEnd('/')}/.default");
					var authRequest = new HttpRequestMessage(HttpMethod.Post, "")
					{
						Content = new StringContent(
							$"grant_type=client_credentials&scope={scope}",
							Encoding.UTF8,
							"application/x-www-form-urlencoded")
					};
					var response = await authHttpClient
						.SendAsync(authRequest)
						.ConfigureAwait(false);
					var responseText = await response
						.Content
						.ReadAsStringAsync()
						.ConfigureAwait(false);
					options.AccessToken = GetBearerToken(responseText);
				}
				request.Headers.Add("Authorization", "Bearer " + options.AccessToken);
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
