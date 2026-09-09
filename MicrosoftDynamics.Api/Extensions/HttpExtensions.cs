namespace MicrosoftDynamics.Api.Extensions;

internal static class HttpExtensions
{
	private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

	/// <summary>
	/// Header names whose values carry a credential and must never be rendered into a log message or
	/// an exception message.
	/// </summary>
	private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Authorization",
		"Proxy-Authorization",
		"Cookie",
		"Set-Cookie",
		"X-API-Key",
		"Api-Key",
		"X-Api-Token",
		"X-Auth-Token",
	};

	/// <summary>
	/// The subset of sensitive headers whose value is of the form "&lt;scheme&gt; &lt;credential&gt;",
	/// where the scheme is safe to keep and useful to see.
	/// </summary>
	private static readonly HashSet<string> SchemePrefixedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Authorization",
		"Proxy-Authorization",
	};

	/// <summary>
	/// Renders headers for diagnostic output, with the value of any credential-bearing header redacted.
	/// </summary>
	internal static string ToDebugString(this HttpHeaders headers)
		=> string.Join("\n", headers.Select(h => $"{h.Key}={RedactIfSensitive(h.Key, h.Value)}"));

	/// <summary>
	/// Joins a header's values, replacing the credential with a redaction marker when the header is a
	/// sensitive one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The authentication scheme and the credential length are preserved. That is enough to tell an
	/// engineer that a credential was sent and roughly what shape it had, which is all diagnosis needs,
	/// without writing the credential itself somewhere it will be retained and widely readable.
	/// </para>
	/// <para>
	/// The scheme is deliberately rendered <em>inside</em> the redaction marker -
	/// "Authorization=&lt;redacted Bearer, length 1842&gt;" rather than
	/// "Authorization=Bearer &lt;redacted, length 1842&gt;". The two carry identical information, but the
	/// latter cannot be told apart from an actual leak by the search anyone uses to hunt for one. Log
	/// stores analyse the message with the standard analyzer, which discards '=', ':' and '&lt;' alike,
	/// so "Authorization=Bearer ..." and "Authorization: Bearer eyJ0..." both tokenise to
	/// "authorization" then "bearer" - exactly the adjacency a phrase query tests for. Opening the
	/// marker first moves the scheme off position 1, which keeps this library's success
	/// distinguishable from its failure. See issue #40: the previous ordering caused a redaction that
	/// was working correctly to be reported as a live credential disclosure.
	/// </para>
	/// </remarks>
	private static string RedactIfSensitive(string name, IEnumerable<string> values)
	{
		var value = string.Join(", ", values);

		if (value.Length == 0 || !SensitiveHeaderNames.Contains(name))
		{
			return value;
		}

		// Only headers whose grammar is "<scheme> <credential>" keep their scheme, so that which
		// authentication mechanism was used remains visible. Applying this to any header containing a
		// space would be unsafe: a cookie such as "session=abc123; HttpOnly" also contains one, and
		// treating the text before it as a scheme would preserve the very value being redacted.
		if (SchemePrefixedHeaderNames.Contains(name))
		{
			var schemeLength = value.IndexOf(' ', StringComparison.Ordinal);

			if (schemeLength > 0)
			{
				return $"<redacted {value[..schemeLength]}, length {value.Length - schemeLength - 1}>";
			}
		}

		return $"<redacted, length {value.Length}>";
	}

	internal static async Task<string> ToDebugStringAsync(this HttpContent? content)
	{
		if (content is null)
		{
			return "No content";
		}

		var contentString = await content
			.ReadAsStringAsync()
			.ConfigureAwait(false);

		return contentString.StartsWith('{')
			? FormatJson(contentString)
			: contentString;
	}

	private static string FormatJson(string json)
	{
		try
		{
			var doc = JsonDocument.Parse(json);
			return JsonSerializer.Serialize(doc, JsonSerializerOptions);
		}
		catch (JsonException)
		{
			return json;
		}
	}
}
