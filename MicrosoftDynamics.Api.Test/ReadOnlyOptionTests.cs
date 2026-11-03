namespace MicrosoftDynamics.Api.Test;

/// <summary>
/// Credential-free tests for <see cref="MicrosoftDynamicsClientOptions.IsReadOnly"/>. No request reaches a network.
/// </summary>
public class ReadOnlyOptionTests
{
	private sealed class StubHandler : HttpMessageHandler
	{
		public int SendCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			SendCount++;
			return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)); // NOSONAR disposed by the caller
		}
	}

	private static MicrosoftDynamicsClientOptions CreateOptions(bool isReadOnly)
		=> new()
		{
			Uri = new Uri("https://readonly-test.invalid/"),
			AccessToken = "not-a-real-token",
			IsReadOnly = isReadOnly
		};

	[Fact]
	public void IsReadOnly_DefaultsToFalse()
		=> new MicrosoftDynamicsClientOptions().IsReadOnly.Should().BeFalse();

	[Fact]
	public async Task PostAsync_WhenReadOnly_ThrowsInvalidOperationException()
	{
		using var client = new MicrosoftDynamicsClient(CreateOptions(true));

		var act = () => client.PostAsync("incidents", new { title = "x" }, CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task PatchAsync_WhenReadOnly_ThrowsInvalidOperationException()
	{
		using var client = new MicrosoftDynamicsClient(CreateOptions(true));

		var act = () => client.PatchAsync("incidents(00000000-0000-0000-0000-000000000001)", new { title = "x" }, CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("PATCH")]
	[InlineData("DELETE")]
	public async Task Handler_WhenReadOnly_ThrowsBeforeSendingWrite(string method)
	{
		using var stub = new StubHandler();
		using var guard = new ReadOnlyGuardHandler(CreateOptions(true), stub);
		using var invoker = new HttpMessageInvoker(guard);

		using var request = new HttpRequestMessage(new HttpMethod(method), "https://readonly-test.invalid/x");
		var act = async () => await invoker.SendAsync(request, CancellationToken.None).ConfigureAwait(false);

		await act.Should().ThrowAsync<InvalidOperationException>();
		stub.SendCount.Should().Be(0);
	}

	[Fact]
	public async Task Handler_WhenReadOnly_AllowsGet()
	{
		using var stub = new StubHandler();
		using var guard = new ReadOnlyGuardHandler(CreateOptions(true), stub);
		using var invoker = new HttpMessageInvoker(guard);

		using var request = new HttpRequestMessage(HttpMethod.Get, "https://readonly-test.invalid/x");
		using var response = await invoker.SendAsync(request, CancellationToken.None);

		response.IsSuccessStatusCode.Should().BeTrue();
		stub.SendCount.Should().Be(1);
	}

	[Fact]
	public async Task Handler_ReadsIsReadOnlyOnEachRequest()
	{
		var options = CreateOptions(false);
		using var stub = new StubHandler();
		using var guard = new ReadOnlyGuardHandler(options, stub);
		using var invoker = new HttpMessageInvoker(guard);

		using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "https://readonly-test.invalid/x");
		using var firstResponse = await invoker.SendAsync(firstRequest, CancellationToken.None);
		options.IsReadOnly = true;
		using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "https://readonly-test.invalid/x");
		var act = async () => await invoker.SendAsync(secondRequest, CancellationToken.None).ConfigureAwait(false);

		await act.Should().ThrowAsync<InvalidOperationException>();
		stub.SendCount.Should().Be(1);
	}
}
