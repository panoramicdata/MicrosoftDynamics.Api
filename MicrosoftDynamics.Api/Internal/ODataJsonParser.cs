namespace MicrosoftDynamics.Api.Internal;

/// <summary>
/// Converts raw OData JSON payloads into loosely typed dictionaries.
/// </summary>
internal static class ODataJsonParser
{
	/// <summary>
	/// Parses the "value" array of an OData collection response.
	/// </summary>
	internal static List<IDictionary<string, object?>> ParseCollection(JsonDocument document)
	{
		var results = new List<IDictionary<string, object?>>();

		if (!document.RootElement.TryGetProperty("value", out var valueArray) || valueArray.ValueKind != JsonValueKind.Array)
		{
			return results;
		}

		foreach (var item in valueArray.EnumerateArray())
		{
			results.Add(ParseEntity(item));
		}

		return results;
	}

	/// <summary>
	/// Parses a single OData entity. Non-object elements yield an empty dictionary.
	/// </summary>
	internal static Dictionary<string, object?> ParseEntity(JsonElement element)
	{
		var dictionary = new Dictionary<string, object?>();

		if (element.ValueKind != JsonValueKind.Object)
		{
			return dictionary;
		}

		foreach (var property in element.EnumerateObject())
		{
			dictionary[property.Name] = ParseValue(property.Value);
		}

		return dictionary;
	}

	private static object? ParseValue(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		JsonValueKind.Null => null,
		JsonValueKind.Object => ParseEntity(element),
		JsonValueKind.Array => element.EnumerateArray().Select(ParseValue).ToList(),
		_ => element.GetRawText()
	};
}
