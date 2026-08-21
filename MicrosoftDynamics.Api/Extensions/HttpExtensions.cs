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
	/// The authentication scheme and the credential length are preserved. That is enough to tell an
	/// engineer that a credential was sent and roughly what shape it had, which is all diagnosis needs,
	/// without writing the credential itself somewhere it will be retained and widely readable.
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
				return $"{value[..schemeLength]} <redacted, length {value.Length - schemeLength - 1}>";
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
