using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KXMapStudio.Core;
using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.Services;

public class MapDataService
{
    private readonly ILogger<MapDataService> _logger;
    private readonly HttpClient _httpClient;
    private Dictionary<int, List<Waypoint>> _waypointsByMap = new();
    private Dictionary<int, Map> _mapData = new();

    public event Action? MapDataRefreshed;

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        LoadWaypointsFromEmbed();
        LoadMapRectsFromEmbed();
    }

    private void LoadWaypointsFromEmbed()
    {
        _logger.LogInformation("Loading waypoints from embedded resource.");
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("KXMapStudio.App.AllWaypoints.json");
            if (stream == null)
            {
                _logger.LogError("Embedded resource 'AllWaypoints.json' not found.");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allWaypoints = JsonSerializer.Deserialize<List<Waypoint>>(json, options);
            if (allWaypoints == null) return;

            _waypointsByMap = allWaypoints
                .GroupBy(w => w.MapId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("Successfully loaded and parsed {Count} waypoints.", allWaypoints.Count);
            MapDataRefreshed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load or parse waypoints from embedded resource.");
        }
    }

    private void LoadMapRectsFromEmbed()
    {
        _logger.LogInformation("Loading map rectangles from embedded resource.");
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("KXMapStudio.App.AllMapRects.json");
            if (stream == null)
            {
                _logger.LogError("Embedded resource 'AllMapRects.json' not found.");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allMapRects = JsonSerializer.Deserialize<List<Map>>(json, options);
            if (allMapRects == null) return;

            _mapData = allMapRects.ToDictionary(m => m.Id);

            _logger.LogInformation("Successfully loaded and parsed {Count} map rectangles.", _mapData.Count);
            MapDataRefreshed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load or parse map rectangles from embedded resource.");
        }
    }

    public List<Waypoint> GetWaypointsForMap(int mapId)
    {
        return _waypointsByMap.TryGetValue(mapId, out var waypoints) ? waypoints : new List<Waypoint>();
    }

    public Map? GetMapData(int mapId)
    {
        return _mapData.TryGetValue(mapId, out var map) ? map : null;
    }

    public string GetWikiUrl(string mapName)
    {
        if (string.IsNullOrEmpty(mapName) || mapName == "Unknown Map")
        {
            return string.Empty;
        }
        return $"https://wiki.guildwars2.com/wiki/{Uri.EscapeDataString(mapName.Replace(' ', '_'))}";
    }
}

public class Map
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("MapRect")]
    public double[][] MapRect { get; set; } = [];

    [JsonPropertyName("ContinentRect")]
    public double[][] ContinentRect { get; set; } = [];
}
