namespace MicrosoftDynamics.Api;

/// <summary>
/// Refuses every request that is not a read while <see cref="MicrosoftDynamicsClientOptions.IsReadOnly"/> is set.
/// </summary>
internal sealed class ReadOnlyGuardHandler(MicrosoftDynamicsClientOptions options, HttpMessageHandler? innerHandler = null)
#pragma warning disable CA2000 // Owned by the DelegatingHandler, disposed with it
	: DelegatingHandler(innerHandler ?? new HttpClientHandler())
#pragma warning restore CA2000
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (options.IsReadOnly
			&& request.Method != HttpMethod.Get
			&& request.Method != HttpMethod.Head
			&& request.Method != HttpMethod.Options)
		{
			throw new InvalidOperationException($"The client is read-only (IsReadOnly is true): {request.Method} requests are not allowed.");
		}

		return base.SendAsync(request, cancellationToken);
	}
}
