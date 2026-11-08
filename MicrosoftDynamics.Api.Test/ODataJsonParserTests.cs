using MicrosoftDynamics.Api.Internal;
using System.Text.Json;

namespace MicrosoftDynamics.Api.Test;

/// <summary>
/// Pure unit tests for turning OData JSON into loosely typed dictionaries; no credentials or tenant needed.
/// </summary>
public class ODataJsonParserTests
{
	[Fact]
	public void ParseCollection_ConvertsEveryJsonValueKind()
	{
		using var document = JsonDocument.Parse("""
			{"value":[{"name":"Contoso","count":3,"ratio":0.5,"active":true,"closed":false,"owner":null,
			"address":{"city":"Leeds"},"tags":["a",1]}]}
			""");

		var entity = ODataJsonParser.ParseCollection(document).Should().ContainSingle().Subject;

		entity["name"].Should().Be("Contoso");
		entity["count"].Should().Be(3L);
		entity["ratio"].Should().Be(0.5);
		entity["active"].Should().Be(true);
		entity["closed"].Should().Be(false);
		entity["owner"].Should().BeNull();
		entity["address"].Should().BeEquivalentTo(new Dictionary<string, object?> { ["city"] = "Leeds" });
		entity["tags"].Should().BeEquivalentTo(new List<object?> { "a", 1L });
	}

	[Theory]
	[InlineData("""{"@odata.context":"x"}""")]
	[InlineData("""{"value":{"name":"not an array"}}""")]
	public void ParseCollection_WithoutAValueArray_IsEmpty(string json)
	{
		using var document = JsonDocument.Parse(json);

		ODataJsonParser.ParseCollection(document).Should().BeEmpty();
	}

	[Fact]
	public void ParseEntity_OfANonObject_IsEmpty()
	{
		using var document = JsonDocument.Parse("[1,2]");

		ODataJsonParser.ParseEntity(document.RootElement).Should().BeEmpty();
	}
}
