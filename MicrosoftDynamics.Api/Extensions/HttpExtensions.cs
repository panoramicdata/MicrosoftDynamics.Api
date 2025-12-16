using System.Text.Json;

namespace MicrosoftDynamics.Api.Extensions;

internal static class HttpExtensions
{
	private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

	internal static string ToDebugString(this HttpHeaders headers)
		=> string.Join("\n", headers.Select(h => $"{h.Key}={string.Join(", ", h.Value)}"));

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
