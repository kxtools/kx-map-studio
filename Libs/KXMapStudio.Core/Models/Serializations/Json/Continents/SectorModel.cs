namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a sector entry inside a continent floor map payload.
/// </summary>
/// <param name="Id">The sector identifier.</param>
/// <param name="Name">The sector name.</param>
/// <param name="Level">The sector level.</param>
/// <param name="Coord">The sector coordinates.</param>
/// <param name="Bounds">The sector bounds polygon.</param>
/// <param name="ChatLink">The in-game chat link.</param>
public sealed record SectorModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("level")] int Level,
	[property: JsonPropertyName("coord")] double[] Coord,
	[property: JsonPropertyName("bounds")] double[][] Bounds,
	[property: JsonPropertyName("chat_link")]
	string ChatLink);