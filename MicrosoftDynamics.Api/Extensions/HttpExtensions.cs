namespace MicrosoftDynamics.Api.Extensions;

internal static class HttpExtensions
{
	internal static string ToDebugString(this HttpHeaders headers)
		=> string.Join("\n", headers.Select(h => $"{h.Key}={string.Join(", ", h.Value)}"));

	internal static async Task<string> ToDebugStringAsync(this HttpContent content)
	{
		string contentString;
		return
			content is null ? "No content" :
			(contentString = await content.ReadAsStringAsync()).StartsWith("{") ? FormatJson(contentString)
			: contentString;
	}

	private static string FormatJson(string json)
		=> JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);

}
