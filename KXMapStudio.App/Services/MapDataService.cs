using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.Services;

public record MapNameEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public class MapDataService
{
    private readonly ILogger<MapDataService> _logger;
    private readonly HttpClient _httpClient;
    private Dictionary<int, string> _mapNames = new();

    private const string ApiUrl = "https://api.guildwars2.com/v1/map_names.json";
    private readonly string _cacheFilePath;

    // Event to notify the UI when data is ready
    public event Action? MapDataRefreshed;

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        // Define a safe, user-specific location for the cache file
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheFilePath = Path.Combine(appDataPath, "KXMapStudio", "map_names.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath)!);
    }

    // This is the main method to be called at app startup
    public async Task InitializeAsync()
    {
        // Load from cache first for a fast startup. This is non-blocking.
        await LoadFromCacheAsync();

        // Then, try to refresh from the API in the background.
        // This won't block the UI and will update the data if successful.
        await FetchAndCacheAsync();
    }

    private async Task FetchAndCacheAsync()
    {
        _logger.LogInformation("Fetching latest map names from API.");
        try
        {
            var mapEntries = await _httpClient.GetFromJsonAsync<List<MapNameEntry>>(ApiUrl);
            if (mapEntries == null)
            {
                _logger.LogWarning("API returned null for map names.");
                return;
            }

            var newMapNames = mapEntries
                .Where(e => int.TryParse(e.Id, out _))
                .ToDictionary(e => int.Parse(e.Id), e => e.Name);

            // If we got new data, update in-memory dict, save to cache, and notify listeners.
            _mapNames = newMapNames;
            await SaveToCacheAsync();
            _logger.LogInformation("Successfully refreshed and cached {Count} map names.", _mapNames.Count);
            MapDataRefreshed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch map names from API. Will rely on cached data if available.");
        }
    }

    private async Task LoadFromCacheAsync()
    {
        if (!File.Exists(_cacheFilePath)) return;

        try
        {
            _logger.LogInformation("Loading map names from local cache: {Path}", _cacheFilePath);
            var json = await File.ReadAllTextAsync(_cacheFilePath);
            var cachedNames = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            if (cachedNames != null)
            {
                _mapNames = cachedNames;
                MapDataRefreshed?.Invoke(); // Notify UI that some data is ready
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load map names from cache file.");
        }
    }

    private async Task SaveToCacheAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_mapNames);
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save map names to cache.");
        }
    }

    public string GetMapName(int mapId)
    {
        return _mapNames.TryGetValue(mapId, out var name) ? name : "Unknown Map";
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
