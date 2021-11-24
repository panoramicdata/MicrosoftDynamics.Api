using MicrosoftDynamics.Api.Extensions;

namespace MicrosoftDynamics.Api;

public class MicrosoftDynamicsClient : ODataClient
{
	private static Uri? _uri;
	public readonly MicrosoftDynamicsClientOptions Options;
	private readonly ILogger _logger;

	public MicrosoftDynamicsClient(MicrosoftDynamicsClientOptions options) : base(GetSettings(options))
	{
		Options = options;
		_logger = options.Logger ?? NullLogger.Instance;
	}

	/// <summary>
	/// This permits updates using @odata.bind.   You will have to add a parameter for the namespace, like:
	/// - "@odata.type": "#Microsoft.Dynamics.CRM.incident"
	/// </summary>
	/// <param name="path"></param>
	/// <param name="entity"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>The resulting body, interpreted as a JObject</returns>
	public async Task<Guid> PostAsync(
		string path,
		object entity,
		CancellationToken cancellationToken)
	{
		var responseMessage = await SendAsync(HttpMethod.Post, path, entity, cancellationToken).ConfigureAwait(false);
		var createdEntityHeader = responseMessage.Headers.GetValues("OData-EntityId").Single();
		var guidString = createdEntityHeader.Split('(').Last().TrimEnd(')');
		return new Guid(guidString);
	}

	/// <summary>
	/// This permits updates using @odata.bind.   You will have to add a parameter for the namespace, like:
	/// - "@odata.type": "#Microsoft.Dynamics.CRM.incident"
	/// </summary>
	/// <param name="path"></param>
	/// <param name="entity"></param>
	/// <param name="cancellationToken"></param>
	public async Task PatchAsync(
		string path,
		object entity,
		CancellationToken cancellationToken)
		=> _ = await SendAsync(new HttpMethod("PATCH"), path, entity, cancellationToken).ConfigureAwait(false);

	private async Task<HttpResponseMessage> SendAsync(
		HttpMethod httpMethod,
		string path,
		object entity,
		CancellationToken cancellationToken)
	{
		using var httpClient = new HttpClient
		{
			BaseAddress = _uri!
		};
		var requestBody = JsonConvert.SerializeObject(entity);
		var request = new HttpRequestMessage(httpMethod, path)
		{
			Content = new StringContent(requestBody,
				Encoding.UTF8,
				"application/json")
		};
		request.Headers.Add("Authorization", "Bearer " + Options.AccessToken);
		_logger.LogDebug(
			"Sending {httpMethod}: {path}: {request}",
			httpMethod,
			path,
			request);
		var responseMessage = await httpClient
			.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		if (!responseMessage.IsSuccessStatusCode)
		{
			var responseBody = await responseMessage
				.Content
				.ReadAsStringAsync()
				.ConfigureAwait(false);
			_logger.LogDebug(
				"Received non-success ({statusCode}): {responseBody}",
				responseMessage.StatusCode,
				responseBody);

			throw new InvalidOperationException(
				$"Path: {_uri!}/{path} {responseMessage.StatusCode}\n" +
				$"Request Headers: {request.Headers}\n" +
				$"Request Body: {requestBody}\n" +
				$"Response Headers: {responseMessage.Headers}\n" +
				$"Response Body: {responseBody}"
				);
		}

		_logger.LogDebug(
			"Received success ({statusCode}): {responseBody}",
			responseMessage.StatusCode,
			await responseMessage
				.Content
				.ReadAsStringAsync()
				.ConfigureAwait(false));
		return responseMessage;
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
			options.Logger.LogDebug(
				"Sending {requestMethod} {requestUri}\n{headers}\n{content}",
				request.Method,
				request.RequestUri,
				(request.Headers as HttpHeaders).ToDebugString(),
				await request.Content.ToDebugStringAsync()
				);
		};

		settings.AfterResponseAsync += async (HttpResponseMessage responseMessage) =>
		{
			if (responseMessage.RequestMessage.RequestUri.ToString().Contains("$metadata"))
			{
				options.Logger.LogTrace(
					"Received {statusCode}\n{headers}\n{responseBody}",
					responseMessage.StatusCode,
					(responseMessage.Headers as HttpHeaders).ToDebugString(),
					await responseMessage.Content.ToDebugStringAsync()
					);
			}
			else
			{
				options.Logger.LogDebug(
					"Received {statusCode}\n{headers}\n{responseBody}",
					responseMessage.StatusCode,
					(responseMessage.Headers as HttpHeaders).ToDebugString(),
					await responseMessage.Content.ToDebugStringAsync()
					);
			}
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
