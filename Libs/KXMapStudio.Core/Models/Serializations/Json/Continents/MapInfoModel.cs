namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents a map entry inside a continent floor payload.
/// </summary>
/// <param name="Id">The map identifier.</param>
/// <param name="Name">The map name.</param>
/// <param name="MinLevel">The minimum level for the map.</param>
/// <param name="MaxLevel">The maximum level for the map.</param>
/// <param name="DefaultFloor">The default floor index.</param>
/// <param name="LabelCoord">The optional label coordinate.</param>
/// <param name="MapRect">The map rectangle in world coordinates.</param>
/// <param name="ContinentRect">The map rectangle in continent coordinates.</param>
/// <param name="PointsOfInterest">The keyed points-of-interest dictionary.</param>
/// <param name="Tasks">The keyed task dictionary.</param>
/// <param name="Sectors">The keyed sector dictionary.</param>
/// <param name="SkillChallenges">The list of skill challenges.</param>
/// <param name="Adventures">The list of adventures.</param>
/// <param name="MasteryPoints">The list of mastery points.</param>
public sealed record MapInfoModel(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("min_level")]
	int MinLevel,
	[property: JsonPropertyName("max_level")]
	int MaxLevel,
	[property: JsonPropertyName("default_floor")]
	int DefaultFloor,
	[property: JsonPropertyName("label_coord")]
	double[]? LabelCoord,
	[property: JsonPropertyName("map_rect")]
	double[][] MapRect,
	[property: JsonPropertyName("continent_rect")]
	double[][] ContinentRect,
	[property: JsonPropertyName("points_of_interest")]
	Dictionary<int, PointOfInterestModel> PointsOfInterest,
	[property: JsonPropertyName("tasks")] Dictionary<int, TaskInfoModel> Tasks,
	[property: JsonPropertyName("sectors")]
	Dictionary<int, SectorModel> Sectors,
	[property: JsonPropertyName("skill_challenges")]
	List<SkillChallengeModel> SkillChallenges,
	[property: JsonPropertyName("adventures")]
	List<AdventureModel> Adventures,
	[property: JsonPropertyName("mastery_points")]
	List<MasteryPointModel> MasteryPoints);