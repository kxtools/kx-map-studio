namespace KXMapStudio.Core.Models.Serializations.Json;

/// <summary>
///     Represents a named coordinate entry inside a KX JSON document.
/// </summary>
/// <param name="Name">The coordinate name/label.</param>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Z">The Z coordinate.</param>
public sealed record CoordinatesModel(
	[property: JsonPropertyName("Name")] string Name,
	[property: JsonPropertyName("X")] double X,
	[property: JsonPropertyName("Y")] double Y,
	[property: JsonPropertyName("Z")] double Z
);