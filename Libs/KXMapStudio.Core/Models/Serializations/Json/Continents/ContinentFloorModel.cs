namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a Guild Wars 2 continent floor payload.
/// </summary>
/// <param name="Id">The floor identifier.</param>
/// <param name="TextureDimensions">The texture dimensions.</param>
/// <param name="ClampedView">The optional clamped view rectangle.</param>
/// <param name="Regions">The keyed region dictionary.</param>
public sealed record ContinentFloorModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("texture_dims")]
	int[] TextureDimensions,
	[property: JsonPropertyName("clamped_view")]
	int[][]? ClampedView,
	[property: JsonPropertyName("regions")]
	Dictionary<int, RegionModel> Regions);