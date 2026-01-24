namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a point of interest entry inside a continent floor payload.
/// </summary>
/// <param name="Id">The point of interest identifier.</param>
/// <param name="Name">The optional point of interest name.</param>
/// <param name="Type">The point of interest type.</param>
/// <param name="Floor">The optional floor index.</param>
/// <param name="Coord">The coordinates.</param>
/// <param name="ChatLink">The in-game chat link.</param>
/// <param name="Icon">The optional icon URL.</param>
public sealed record PointOfInterestModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("name")] string? Name,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("floor")] int? Floor,
	[property: JsonPropertyName("coord")] double[] Coord,
	[property: JsonPropertyName("chat_link")]
	string ChatLink,
	[property: JsonPropertyName("icon")] string? Icon);