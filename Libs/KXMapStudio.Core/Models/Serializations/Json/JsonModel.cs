namespace KXMapStudio.Core.Models.Serializations.Json;

/// <summary>
///     Represents the root KX JSON document used by KX Map Studio.
/// </summary>
/// <param name="Name">The document name.</param>
/// <param name="Author">The optional author name.</param>
/// <param name="Coordinates">The list of coordinate entries.</param>
public sealed record JsonModel(
	[property: JsonPropertyName("Name")] string Name,
	[property: JsonPropertyName("Author")] string? Author,
	[property: JsonPropertyName("Coordinates")]
	CoordinatesModel[] Coordinates
);