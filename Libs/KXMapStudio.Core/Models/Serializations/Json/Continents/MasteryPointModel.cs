namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a mastery point entry inside a continent floor payload.
/// </summary>
/// <param name="Id">The mastery point identifier.</param>
/// <param name="Region">The mastery point region string.</param>
/// <param name="Coord">The mastery point coordinates.</param>
public sealed record MasteryPointModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("region")] string Region,
	[property: JsonPropertyName("coord")] double[] Coord);