using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

using KXMapStudio.App.Models;

using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.Services;

public class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;

    private const string GitHubApiUrl = "https://api.github.com/repos/kxtools/kx-map-studio/releases/latest";

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        // The GitHub API requires a User-Agent header.
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "KXMapStudio");
    }

    public async Task<(bool IsNewVersionAvailable, GitHubRelease? LatestRelease)> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for application updates from {Url}", GitHubApiUrl);

            var latestRelease = await _httpClient.GetFromJsonAsync<GitHubRelease>(GitHubApiUrl);
            if (latestRelease == null || string.IsNullOrEmpty(latestRelease.TagName))
            {
                _logger.LogWarning("Failed to deserialize GitHub release information or tag name is empty.");
                return (false, null);
            }

            // The release tag should be in a format like "v0.1.0". We remove the "v" to parse it.
            var latestVersionString = latestRelease.TagName.TrimStart('v');
            if (!Version.TryParse(latestVersionString, out var latestVersion))
            {
                _logger.LogError("Could not parse latest version from tag: {Tag}", latestRelease.TagName);
                return (false, null);
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion == null)
            {
                _logger.LogError("Could not determine current application version.");
                return (false, null);
            }

            _logger.LogInformation("Current version: {CurrentVersion}, Latest version: {LatestVersion}", currentVersion, latestVersion);

            if (latestVersion > currentVersion)
            {
                _logger.LogInformation("A new version is available: {LatestVersion}", latestVersion);
                return (true, latestRelease);
            }

            _logger.LogInformation("Application is up to date.");
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking for updates.");
            return (false, null);
        }
    }
}
