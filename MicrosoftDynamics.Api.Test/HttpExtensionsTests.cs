using MicrosoftDynamics.Api.Extensions;
using System.Net.Http.Headers;

namespace MicrosoftDynamics.Api.Test;

/// <summary>
/// Tests for header redaction in diagnostic output.
///
/// <para>
/// The client sets an Authorization header carrying a bearer token on every request. Any code path that
/// renders headers into a log message or an exception message therefore writes a usable access token
/// wherever those messages end up. These tests pin the redaction that prevents it.
/// </para>
///
/// <para>
/// These are pure unit tests. They construct headers directly and require no credentials, no
/// configuration and no live tenant.
/// </para>
/// </summary>
public class HttpExtensionsTests
{
	/// <summary>
	/// Shaped like a real JWT so that a partial-redaction bug would be visible, but not a real token.
	/// </summary>
	private static readonly string FakeJwt = string.Join(
		'.',
		"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9",
		"eyJzdWIiOiJ0ZXN0Iiwibm90IjoicmVhbCJ9",
		"c2lnbmF0dXJlLW5vdC1yZWFs");

	[Fact]
	public void ToDebugString_BearerToken_DoesNotLeakTheCredential()
	{
		using var request = new HttpRequestMessage();
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwt);

		var debugString = request.Headers.ToDebugString();

		debugString.Should().NotContain(FakeJwt);
		debugString.Should().NotContain("c2lnbmF0dXJlLW5vdC1yZWFs");
		debugString.Should().NotContain("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9");
	}

	[Fact]
	public void ToDebugString_BearerToken_KeepsTheSchemeAndLength()
	{
		using var request = new HttpRequestMessage();
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwt);

		var debugString = request.Headers.ToDebugString();

		debugString.Should().Be($"Authorization=Bearer <redacted, length {FakeJwt.Length}>");
	}

	[Fact]
	public void ToDebugString_BasicScheme_KeepsTheSchemeAndRedactsTheCredential()
	{
		using var request = new HttpRequestMessage();
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNzd29yZA==");

		var debugString = request.Headers.ToDebugString();

		debugString.Should().Be("Authorization=Basic <redacted, length 20>");
		debugString.Should().NotContain("dXNlcjpwYXNzd29yZA==");
	}

	/// <summary>
	/// A header added without validation keeps whatever casing the caller used, so redaction must not
	/// depend on the header name being canonically cased.
	/// </summary>
	[Theory]
	[InlineData("authorization")]
	[InlineData("AUTHORIZATION")]
	[InlineData("AuThOrIzAtIoN")]
	public void ToDebugString_AuthorizationHeader_IsRedactedWhateverTheCasing(string headerName)
	{
		using var request = new HttpRequestMessage();
		request.Headers.TryAddWithoutValidation(headerName, $"Bearer {FakeJwt}");

		var debugString = request.Headers.ToDebugString();

		debugString.Should().NotContain(FakeJwt);
		debugString.Should().Contain("<redacted");
	}

	[Theory]
	[InlineData("Proxy-Authorization")]
	[InlineData("Cookie")]
	[InlineData("X-API-Key")]
	[InlineData("Api-Key")]
	[InlineData("X-Api-Token")]
	[InlineData("X-Auth-Token")]
	public void ToDebugString_OtherCredentialHeaders_AreRedacted(string headerName)
	{
		const string secret = "s3cr3t-value-that-must-not-be-logged";
		using var request = new HttpRequestMessage();
		request.Headers.TryAddWithoutValidation(headerName, secret);

		var debugString = request.Headers.ToDebugString();

		debugString.Should().NotContain(secret);
		debugString.Should().Contain("<redacted");
	}

	/// <summary>
	/// A credential with no scheme prefix has nothing safe to preserve, so all of it goes.
	/// </summary>
	[Fact]
	public void ToDebugString_CredentialWithoutAScheme_IsRedactedEntirely()
	{
		using var request = new HttpRequestMessage();
		request.Headers.TryAddWithoutValidation("X-API-Key", "abcdef123456");

		var debugString = request.Headers.ToDebugString();

		debugString.Should().Be("X-API-Key=<redacted, length 12>");
	}

	[Fact]
	public void ToDebugString_NonSensitiveHeader_IsUnchanged()
	{
		using var request = new HttpRequestMessage();
		request.Headers.TryAddWithoutValidation("traceparent", "00-abc123-def456-00");

		var debugString = request.Headers.ToDebugString();

		debugString.Should().Be("traceparent=00-abc123-def456-00");
	}

	/// <summary>
	/// Redaction must be surgical: the diagnostically useful headers alongside the credential are what
	/// make a failure message worth reading, so they must survive intact.
	/// </summary>
	[Fact]
	public void ToDebugString_RedactsOnlyTheSensitiveHeader()
	{
		using var request = new HttpRequestMessage();
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwt);
		request.Headers.TryAddWithoutValidation("traceparent", "00-abc123-def456-00");
		request.Headers.TryAddWithoutValidation("Request-Id", "|abc.def.");

		var debugString = request.Headers.ToDebugString();

		debugString.Should().NotContain(FakeJwt);
		debugString.Should().Contain("traceparent=00-abc123-def456-00");
		debugString.Should().Contain("Request-Id=|abc.def.");
	}

	[Fact]
	public void ToDebugString_NoHeaders_IsEmpty()
	{
		using var request = new HttpRequestMessage();

		request.Headers.ToDebugString().Should().BeEmpty();
	}

	/// <summary>
	/// Response headers go through the same helper, so Set-Cookie is covered too.
	/// </summary>
	[Fact]
	public void ToDebugString_ResponseSetCookie_IsRedacted()
	{
		using var response = new HttpResponseMessage();
		response.Headers.TryAddWithoutValidation("Set-Cookie", "session=abc123def456; HttpOnly");

		var debugString = response.Headers.ToDebugString();

		debugString.Should().NotContain("abc123def456");
		debugString.Should().Contain("<redacted");
	}
}
