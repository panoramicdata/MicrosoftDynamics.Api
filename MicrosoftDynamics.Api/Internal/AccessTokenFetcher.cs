namespace MicrosoftDynamics.Api.Internal;

/// <summary>
/// Performs the client credentials grant against the configured authentication endpoint.
/// </summary>
internal static class AccessTokenFetcher
{
	/// <summary>
	/// Requests a fresh bearer token.
	/// </summary>
	/// <exception cref="InvalidOperationException">The token endpoint failed, or returned an unusable response.</exception>
	internal static async Task<BearerTokenResponse> FetchAsync(
		MicrosoftDynamicsClientOptions options,
		CancellationToken cancellationToken)
	{
		using var authHttpClient = new HttpClient
		{
			BaseAddress = options.AuthenticationUri
		};
		authHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
			"Basic",
			Base64Encode($"{options.ClientId}:{options.ClientSecret}"));

		var scope = Uri.EscapeDataString($"{options.Uri!.ToString().TrimEnd('/')}/.default");
		using var authRequest = new HttpRequestMessage(HttpMethod.Post, "")
		{
			Content = new StringContent(
				$"grant_type=client_credentials&scope={scope}",
				Encoding.UTF8,
				new MediaTypeHeaderValue("application/x-www-form-urlencoded"))
		};

		using var response = await authHttpClient
			.SendAsync(authRequest, cancellationToken)
			.ConfigureAwait(false);

		var responseText = await response
			.Content
			.ReadAsStringAsync(cancellationToken)
			.ConfigureAwait(false);

		return !response.IsSuccessStatusCode
			? throw new InvalidOperationException($"Unable to fetch the access token. {response.StatusCode} {responseText}")
			: JsonSerializer.Deserialize<BearerTokenResponse>(responseText)
				?? throw new InvalidOperationException("Unable to fetch the access token.");
	}

	private static string Base64Encode(string plainText)
		=> Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
}
