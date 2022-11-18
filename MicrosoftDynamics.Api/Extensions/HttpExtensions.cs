namespace MicrosoftDynamics.Api.Extensions;

internal static class HttpExtensions
{
	internal static string ToDebugString(this HttpHeaders headers)
		=> string.Join("\n", headers.Select(h => $"{h.Key}={string.Join(", ", h.Value)}"));

	internal static async Task<string> ToDebugStringAsync(this HttpContent content)
	{
		if (content is null)
		{
			return "No content";
		}

		var contentString = await content
			.ReadAsStringAsync()
			.ConfigureAwait(false);

		return contentString.StartsWith("{", StringComparison.Ordinal)
			? FormatJson(contentString)
			: contentString;
	}

	private static string FormatJson(string json)
		=> JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);

}
