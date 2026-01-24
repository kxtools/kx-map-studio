namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a region inside a Guild Wars 2 continent floor payload.
/// </summary>
/// <param name="Id">The region identifier.</param>
/// <param name="Name">The region name.</param>
/// <param name="LabelCoord">The label coordinates.</param>
/// <param name="ContinentRect">The continent coordinate rectangle for the region.</param>
/// <param name="Maps">The keyed maps dictionary.</param>
public sealed record RegionModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("label_coord")]
	double[] LabelCoord,
	[property: JsonPropertyName("continent_rect")]
	double[][] ContinentRect,
	[property: JsonPropertyName("maps")] Dictionary<int, MapInfoModel> Maps);