using MicrosoftDynamics.Api.Extensions;
using MicrosoftDynamics.Api.Internal;

namespace MicrosoftDynamics.Api;

public class MicrosoftDynamicsClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private DateTime? _accessTokenExpiryDateTimeUtc;
	private bool _disposed;

	public MicrosoftDynamicsClientOptions Options { get; }

	private bool AccessTokenNeedsRefresh
		=> Options.AccessToken is null
		|| (_accessTokenExpiryDateTimeUtc is not null && _accessTokenExpiryDateTimeUtc < DateTime.UtcNow);

	public MicrosoftDynamicsClient(MicrosoftDynamicsClientOptions options)
	{
		Options = options ?? throw new ArgumentNullException(nameof(options));

		// Validate the options
		Options.Validate();

		var baseUri = new Uri(Options.Uri!, $"api/data/v{Options.OdataApiVersion}/");

		_httpClient = new HttpClient
		{
			BaseAddress = baseUri
		};

		var oDataClientOptions = new ODataClientOptions
		{
			BaseUrl = baseUri.ToString(),
			HttpClient = _httpClient,
			Logger = Options.Logger,
			ConfigureRequest = request =>
			{
				// Synchronously ensure we have a valid token and set the header
				EnsureAccessTokenUpdatedSync();
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.AccessToken);

				if (Options.Logger.IsEnabled(LogLevel.Debug))
				{
					Options.Logger.LogDebug(
						"Sending {RequestMethod} {RequestUri}",
						request.Method,
						request.RequestUri
					);
				}
			}
		};

		ODataClient = new ODataClient(oDataClientOptions);
	}

	/// <summary>
	/// Gets the underlying OData client for advanced operations.
	/// </summary>
	public ODataClient ODataClient { get; }

	#region Backward Compatibility Methods

	/// <summary>
	/// Clears the OData client metadata cache. This is a no-op as the new OData client does not cache metadata.
	/// </summary>
	public void ClearODataClientMetaDataCache() => ODataClient.InvalidateMetadataCache();

	/// <summary>
	/// Finds multiple entries using a raw OData query string.
	/// </summary>
	/// <param name="query">The raw OData query (e.g., "incidents?$filter=statecode eq 0").</param>
	/// <returns>The matching entities as dictionaries.</returns>
	public Task<IEnumerable<IDictionary<string, object?>>> FindEntriesAsync(string query)
		=> FindEntriesAsync(query, CancellationToken.None);

	/// <summary>
	/// Finds multiple entries using a raw OData query string.
	/// </summary>
	/// <param name="query">The raw OData query (e.g., "incidents?$filter=statecode eq 0").</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matching entities as dictionaries.</returns>
	public async Task<IEnumerable<IDictionary<string, object?>>> FindEntriesAsync(
		string query,
		CancellationToken cancellationToken)
	{
		var document = await ODataClient.GetRawAsync(query, null, cancellationToken).ConfigureAwait(false);
		return ODataJsonParser.ParseCollection(document);
	}

	/// <summary>
	/// Finds a single entry using a raw OData query string.
	/// </summary>
	/// <param name="query">The raw OData query (e.g., "incidents(guid)").</param>
	/// <returns>The entity as a dictionary, or null if not found.</returns>
	public Task<IDictionary<string, object?>?> FindEntryAsync(string query)
		=> FindEntryAsync(query, CancellationToken.None);

	/// <summary>
	/// Finds a single entry using a raw OData query string.
	/// </summary>
	/// <param name="query">The raw OData query (e.g., "incidents(guid)").</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The entity as a dictionary, or null if not found.</returns>
	public async Task<IDictionary<string, object?>?> FindEntryAsync(
		string query,
		CancellationToken cancellationToken)
	{
		var document = await ODataClient.GetRawAsync(query, null, cancellationToken).ConfigureAwait(false);
		return ODataJsonParser.ParseEntity(document.RootElement);
	}

	#endregion

	/// <summary>
	/// Ensure the client has an access token, which can then be used in normal HttpClient requests i.e. not using the client
	/// </summary>
	/// <returns>The access token</returns>
	/// <exception cref="HttpRequestException"></exception>
	public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
	{
		if (AccessTokenNeedsRefresh)
		{
			await EnsureAccessTokenUpdatedAsync(cancellationToken)
				.ConfigureAwait(false);
		}

		return Options.AccessToken ?? throw new HttpRequestException("Unable to fetch the access token.");
	}

	/// <summary>
	/// Gets all entities matching the query, following pagination.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <returns>All matching entities.</returns>
	public Task<ODataResponse<T>> GetAllAsync<T>()
		where T : class
		=> GetAllAsync<T>(null, CancellationToken.None);

	/// <summary>
	/// Gets all entities matching the query, following pagination.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">Entity set name override.</param>
	/// <returns>All matching entities.</returns>
	public Task<ODataResponse<T>> GetAllAsync<T>(string? entitySet)
		where T : class
		=> GetAllAsync<T>(entitySet, CancellationToken.None);

	/// <summary>
	/// Gets all entities matching the query, following pagination.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">Entity set name override.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>All matching entities.</returns>
	public Task<ODataResponse<T>> GetAllAsync<T>(string? entitySet, CancellationToken cancellationToken)
		where T : class
		=> entitySet is null
			? ODataClient.GetAllAsync(ODataClient.For<T>(), cancellationToken)
			: ODataClient.GetAllAsync(ODataClient.For<T>(entitySet), cancellationToken);

	/// <summary>
	/// Gets a page of entities matching the query.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="queryBuilder">The query builder.</param>
	/// <returns>A page of matching entities.</returns>
	public Task<ODataResponse<T>> GetAsync<T>(ODataQueryBuilder<T> queryBuilder)
		where T : class
		=> GetAsync(queryBuilder, CancellationToken.None);

	/// <summary>
	/// Gets a page of entities matching the query.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="queryBuilder">The query builder.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A page of matching entities.</returns>
	public Task<ODataResponse<T>> GetAsync<T>(ODataQueryBuilder<T> queryBuilder, CancellationToken cancellationToken)
		where T : class
		=> ODataClient.GetAsync(queryBuilder, cancellationToken);

	/// <summary>
	/// Creates a query builder for the specified entity type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <returns>A query builder for building OData queries.</returns>
	public ODataQueryBuilder<T> For<T>()
		where T : class
		=> For<T>(null);

	/// <summary>
	/// Creates a query builder for the specified entity type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">Entity set name override.</param>
	/// <returns>A query builder for building OData queries.</returns>
	public ODataQueryBuilder<T> For<T>(string? entitySet)
		where T : class
		=> entitySet is null
			? ODataClient.For<T>()
			: ODataClient.For<T>(entitySet);

	/// <summary>
	/// Creates a query builder for the specified entity type.
	/// </summary>
	/// <param name="entitySet">Optional entity set name override.</param>
	/// <returns>A query builder for building OData queries.</returns>
	public FluentODataQueryBuilder For(string entitySet)
		=> ODataClient.For(entitySet);

	/// <summary>
	/// Creates an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="entity">The entity to create.</param>
	/// <returns>The created entity.</returns>
	public Task<T> CreateAsync<T>(string entitySet, T entity)
		where T : class
		=> CreateAsync(entitySet, entity, CancellationToken.None);

	/// <summary>
	/// Creates an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="entity">The entity to create.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The created entity.</returns>
	public Task<T> CreateAsync<T>(string entitySet, T entity, CancellationToken cancellationToken)
		where T : class
		=> ODataClient.CreateAsync(entitySet, entity, null, cancellationToken);

	/// <summary>
	/// Updates an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="entity">The entity updates.</param>
	public Task UpdateAsync<T>(string entitySet, object key, object entity)
		where T : class
		=> UpdateAsync<T>(entitySet, key, entity, CancellationToken.None);

	/// <summary>
	/// Updates an entity.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="entity">The entity updates.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public Task UpdateAsync<T>(string entitySet, object key, object entity, CancellationToken cancellationToken)
		where T : class
		=> ODataClient.UpdateAsync<T>(entitySet, key, entity, null, cancellationToken);

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	public Task DeleteAsync(string entitySet, object key)
		=> DeleteAsync(entitySet, key, CancellationToken.None);

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	/// <param name="entitySet">The entity set name.</param>
	/// <param name="key">The entity key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public Task DeleteAsync(string entitySet, object key, CancellationToken cancellationToken)
		=> ODataClient.DeleteAsync(entitySet, key, null, cancellationToken);

	/// <summary>
	/// Creates a batch builder for executing multiple operations in a single request.
	/// </summary>
	/// <returns>A batch builder.</returns>
	public ODataBatchBuilder CreateBatch()
		=> ODataClient.CreateBatch();

	/// <summary>
	/// Gets the OData service metadata from the $metadata endpoint.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The parsed OData metadata.</returns>
	public Task<ODataMetadata> GetMetadataAsync(CancellationToken cancellationToken)
		=> ODataClient.GetMetadataAsync(cancellationToken);

	/// <summary>
	/// Gets the OData service metadata from the $metadata endpoint.
	/// </summary>
	/// <param name="cacheHandling">Cache handling strategy.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The parsed OData metadata.</returns>
	public Task<ODataMetadata> GetMetadataAsync(CacheHandling cacheHandling, CancellationToken cancellationToken)
		=> ODataClient.GetMetadataAsync(cacheHandling, cancellationToken);

	/// <summary>
	/// This permits updates using @odata.bind. You will have to add a parameter for the namespace, like:
	/// - "@odata.type": "#Microsoft.Dynamics.CRM.incident"
	/// </summary>
	/// <param name="path"></param>
	/// <param name="entity"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>The GUID of the created entity</returns>
	public async Task<Guid> PostAsync(
		string path,
		object entity,
		CancellationToken cancellationToken)
	{
		var responseMessage = await SendAsync(HttpMethod.Post, path, entity, cancellationToken).ConfigureAwait(false);
		var createdEntityHeader = responseMessage
			.Headers
			.GetValues("OData-EntityId")
			.Single();
		var guidString = createdEntityHeader
			.Split('(')
			.Last()
			.TrimEnd(')');
		return new Guid(guidString);
	}

	/// <summary>
	/// This permits updates using @odata.bind. You will have to add a parameter for the namespace, like:
	/// - "@odata.type": "#Microsoft.Dynamics.CRM.incident"
	/// </summary>
	/// <param name="path"></param>
	/// <param name="entity"></param>
	/// <param name="cancellationToken"></param>
	public async Task PatchAsync(
		string path,
		object entity,
		CancellationToken cancellationToken)
		=> _ = await SendAsync(HttpMethod.Patch, path, entity, cancellationToken).ConfigureAwait(false);

	private async Task<HttpResponseMessage> SendAsync(
		HttpMethod httpMethod,
		string path,
		object entity,
		CancellationToken cancellationToken)
	{
		var requestBody = JsonSerializer.Serialize(entity);
		using var request = new HttpRequestMessage(httpMethod, path)
		{
			Content = new StringContent(requestBody, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
		};

		await UpdateRequestHeadersAndLogAsync(request, cancellationToken)
			.ConfigureAwait(false);

		var responseMessage = await _httpClient
			.SendAsync(request, cancellationToken)
			.ConfigureAwait(false);

		await LogResponseAsync(responseMessage)
			.ConfigureAwait(false);

		if (!responseMessage.IsSuccessStatusCode)
		{
			var responseBody = await responseMessage
				.Content
				.ReadAsStringAsync(cancellationToken)
				.ConfigureAwait(false);

			// Headers are rendered via ToDebugString so that credential-bearing headers are redacted.
			// Interpolating the header collections directly would write the Authorization header, and
			// therefore a usable access token, into the exception message and from there into any log.
			throw new InvalidOperationException(
				$"Path: {_httpClient.BaseAddress}{path} {responseMessage.StatusCode}\n" +
				$"Request Headers: {request.Headers.ToDebugString()}\n" +
				$"Request Body: {requestBody}\n" +
				$"Response Headers: {responseMessage.Headers.ToDebugString()}\n" +
				$"Response Body: {responseBody}"
				);
		}

		return responseMessage;
	}

	private async Task LogResponseAsync(HttpResponseMessage responseMessage)
	{
		var isMetadataResponse = responseMessage
			.RequestMessage?
			.RequestUri?
			.ToString()
			.Contains("$metadata", StringComparison.Ordinal) == true;

		// Metadata bodies are large and rarely change, so they are logged only when explicitly requested.
		if (isMetadataResponse && !Options.LogMetadata)
		{
			Options.Logger.LogTrace("Metadata received");
			return;
		}

		var logLevel = isMetadataResponse ? LogLevel.Trace : LogLevel.Debug;
		if (!Options.Logger.IsEnabled(logLevel))
		{
			return;
		}

		Options.Logger.Log(
			logLevel,
			"Received {StatusCode}\n{Headers}\n{ResponseBody}",
			responseMessage.StatusCode,
			responseMessage.Headers.ToDebugString(),
			await responseMessage
				.Content
				.ToDebugStringAsync()
				.ConfigureAwait(false)
		);
	}

	private async Task UpdateRequestHeadersAndLogAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (AccessTokenNeedsRefresh)
		{
			await EnsureAccessTokenUpdatedAsync(cancellationToken)
				.ConfigureAwait(false);
		}

		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.AccessToken);
		if (Options.Logger.IsEnabled(LogLevel.Debug))
		{
			Options.Logger.LogDebug(
				"Sending {RequestMethod} {RequestUri}\n{Headers}\n{Content}",
				request.Method,
				request.RequestUri,
				request.Headers.ToDebugString(),
				await request.Content.ToDebugStringAsync().ConfigureAwait(false)
			);
		}
	}

	/// <summary>
	/// Synchronous version of token update for use in ConfigureRequest callback.
	/// </summary>
	private void EnsureAccessTokenUpdatedSync()
	{
		if (AccessTokenNeedsRefresh)
		{
			// Use Task.Run to avoid deadlock on sync-over-async
			Task.Run(async () => await EnsureAccessTokenUpdatedAsync(CancellationToken.None).ConfigureAwait(false))
				.GetAwaiter()
				.GetResult();
		}
	}

	private async Task EnsureAccessTokenUpdatedAsync(CancellationToken cancellationToken)
	{
		var bearerTokenResponse = await AccessTokenFetcher
			.FetchAsync(Options, cancellationToken)
			.ConfigureAwait(false);

		Options.AccessToken = bearerTokenResponse.AccessToken;
		_accessTokenExpiryDateTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Max(0, bearerTokenResponse.ExpiresIn - 10));
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
				(ODataClient as IDisposable)?.Dispose();
				_httpClient.Dispose();
			}

			_disposed = true;
		}
	}
}
