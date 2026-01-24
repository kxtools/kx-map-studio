namespace KXMapStudio.Libs.Services;

/// <summary>
///     Service for checking application updates from GitHub.
/// </summary>
/// <remarks>
///     This service queries the GitHub API to determine if a newer version of the application is available,
///     and provides the URL and version information for the latest release.
/// </remarks>
public sealed partial class UpdateCheckerService : ObservableObject, IUpdateCheckerService
{
	private readonly IGithubHttpClient _githubClient;
	private readonly ILogger<UpdateCheckerService> _logger;

	/// <summary>
	///     Gets or sets a value indicating whether an update is available.
	/// </summary>
	[ObservableProperty] private bool _isUpdateAvailable;

	/// <summary>
	///     Gets or sets the tag name of the latest version (e.g., "v1.2.3").
	/// </summary>
	[ObservableProperty] private string? _latestVersionTag;

	/// <summary>
	///     Gets or sets the URL of the latest release on GitHub.
	/// </summary>
	[ObservableProperty] private string? _latestVersionUrl;

	/// <summary>
	///     Initializes a new instance of the <see cref="UpdateCheckerService" /> class.
	/// </summary>
	/// <param name="githubClient">The GitHub HTTP client for querying release information.</param>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="githubClient" /> or <paramref name="logger" /> is <see langword="null" />.
	/// </exception>
	public UpdateCheckerService(IGithubHttpClient githubClient, ILogger<UpdateCheckerService> logger)
	{
		ArgumentNullException.ThrowIfNull(githubClient);
		ArgumentNullException.ThrowIfNull(logger);

		_githubClient = githubClient;
		_logger = logger;
	}

	/// <summary>
	///     Asynchronously checks for updates from GitHub.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogInformation("Checking for application updates from GitHub...");

			var latestRelease = await _githubClient.GetLatestReleaseAsync(cancellationToken);
			if (latestRelease == null)
			{
				_logger.LogWarning("Failed to retrieve latest release information from GitHub.");
				return;
			}

			var latestVersionString = latestRelease.TagName.TrimStart('v');
			if (!Version.TryParse(latestVersionString, out var latestVersion))
			{
				_logger.LogWarning("Failed to parse version from tag: {TagName}", latestRelease.TagName);
				return;
			}

			var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
			if (currentVersion == null)
			{
				_logger.LogWarning("Failed to retrieve current application version.");
				return;
			}

			_logger.LogInformation("Current version: {CurrentVersion}, Latest version: {LatestVersion}",
				currentVersion, latestVersion);

			if (latestVersion > currentVersion)
			{
				IsUpdateAvailable = true;
				LatestVersionUrl = latestRelease.HtmlUrl;
				LatestVersionTag = latestRelease.TagName;

				_logger.LogInformation("Update available: {LatestVersion} at {Url}",
					latestRelease.TagName, latestRelease.HtmlUrl);
			}
			else
			{
				_logger.LogInformation("Application is up to date.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking for updates from GitHub.");
		}
	}
}