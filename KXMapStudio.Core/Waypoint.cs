
using System.Text.Json.Serialization;

namespace KXMapStudio.Core;

public class Waypoint
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Coord")]
    public double[] Coord { get; set; } = [];

    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("MapId")]
    public int MapId { get; set; }

    [JsonPropertyName("ChatLink")]
    public string ChatLink { get; set; } = string.Empty;
}
