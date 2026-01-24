namespace KXMapStudio.Core.Models.Serializations.Json.Maps;

/// <summary>
///     Represents a Guild Wars 2 map entry returned by the GW2 API.
/// </summary>
/// <param name="Id">The map identifier.</param>
/// <param name="Name">The map name.</param>
/// <param name="MinLevel">The minimum level for the map.</param>
/// <param name="MaxLevel">The maximum level for the map.</param>
/// <param name="DefaultFloor">The default floor index.</param>
/// <param name="Type">The map type.</param>
/// <param name="Floors">The available floor indices.</param>
/// <param name="RegionId">The region identifier.</param>
/// <param name="RegionName">The region name.</param>
/// <param name="ContinentId">The continent identifier.</param>
/// <param name="ContinentName">The continent name.</param>
/// <param name="MapRect">The world coordinate rectangle for the map.</param>
/// <param name="ContinentRect">The continent coordinate rectangle for the map.</param>
public sealed record MapModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("min_level")]
	int MinLevel,
	[property: JsonPropertyName("max_level")]
	int MaxLevel,
	[property: JsonPropertyName("default_floor")]
	int DefaultFloor,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("floors")] int[] Floors,
	[property: JsonPropertyName("region_id")]
	int RegionId,
	[property: JsonPropertyName("region_name")]
	string RegionName,
	[property: JsonPropertyName("continent_id")]
	int ContinentId,
	[property: JsonPropertyName("continent_name")]
	string ContinentName,
	[property: JsonPropertyName("map_rect")]
	int[][] MapRect,
	[property: JsonPropertyName("continent_rect")]
	int[][] ContinentRect);