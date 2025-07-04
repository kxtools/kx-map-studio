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

    private const string MapsApiUrl = "https://api.guildwars2.com/v2/maps?ids=all";

    public event Action? MapDataRefreshed;

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        LoadWaypointsFromEmbed();
        await FetchAndCacheMapDataAsync();
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

    private async Task FetchAndCacheMapDataAsync()
    {
        _logger.LogInformation("Fetching latest map data from API.");
        try
        {
            var maps = await _httpClient.GetFromJsonAsync<List<Map>>(MapsApiUrl);
            if (maps == null) return;

            _mapData = maps.ToDictionary(m => m.Id);

            _logger.LogInformation("Successfully fetched and cached {Count} maps.", _mapData.Count);
            MapDataRefreshed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch map data from API.");
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
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("map_rect")]
    public double[][] MapRect { get; set; } = [];

    [JsonPropertyName("continent_rect")]
    public double[][] ContinentRect { get; set; } = [];
}
