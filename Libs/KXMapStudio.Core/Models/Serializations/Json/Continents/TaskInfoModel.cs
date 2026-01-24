namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a renown heart task entry inside a continent floor payload.
/// </summary>
/// <param name="Id">The task identifier.</param>
/// <param name="Objective">The objective description.</param>
/// <param name="Level">The recommended level.</param>
/// <param name="Coord">The task coordinates.</param>
public sealed record TaskInfoModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("objective")]
	string Objective,
	[property: JsonPropertyName("level")] int Level,
	[property: JsonPropertyName("coord")] double[] Coord);