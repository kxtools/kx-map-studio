namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a skill challenge entry inside a continent floor payload.
/// </summary>
/// <param name="Id">The skill challenge identifier.</param>
/// <param name="Coord">The skill challenge coordinates.</param>
public sealed record SkillChallengeModel(
	[property: JsonPropertyName("id")] string Id,
	[property: JsonPropertyName("coord")] double[] Coord);