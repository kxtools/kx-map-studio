namespace KXMapStudio.Libs.ViewModels.BottomSide.StatusBar;

/// <summary>
///     ViewModel for the status bar, displaying Mumble connection state, player position, and external links.
/// </summary>
/// <remarks>
///     <para>
///         This ViewModel subscribes to <see cref="IMumbleService.MumbleUpdated" /> events and updates
///         UI-bound properties to reflect real-time player state from Guild Wars 2 via Mumble Link.
///     </para>
///     <para>
///         Commands provide quick access to external resources (website, Discord, GitHub).
///         Implements <see cref="IDisposable" /> to properly unsubscribe from Mumble events on disposal.
///     </para>
/// </remarks>
public sealed partial class StatusBarViewModel : ObservableObject, IStatusBarViewModel, IDisposable
{
	private readonly IDispatcherHelper _dispatcherHelper;
	private readonly IGw2DataCacheService _gw2DataCache;
	private readonly ILogger<StatusBarViewModel> _logger;
	private readonly IMumbleService _mumbleService;
	private readonly INotificationService _notificationService;
	private readonly IUpdateCheckerService _updateChecker;

	/// <summary>
	///     Gets or sets the character name displayed in the status bar.
	/// </summary>
	[ObservableProperty] private string _characterName = "Not connected";

	/// <summary>
	///     Gets or sets the current Mumble connection state.
	/// </summary>
	[ObservableProperty] private MumbleConnectionState _connectionState = MumbleConnectionState.Disconnected;

	/// <summary>
	///     Gets or sets the formatted coordinates text (e.g., "Pos: 123.45, 67.89, 10.11").
	/// </summary>
	[ObservableProperty] private string _coordinatesText = "Pos: N/A";

	/// <summary>
	///     Gets or sets the formatted map ID text (e.g., "Map: 1234").
	/// </summary>
	[ObservableProperty] private string _mapText = "Map: N/A";

	/// <summary>
	///     Gets or sets the formatted X coordinate text (e.g., "X: 123.45").
	/// </summary>
	[ObservableProperty] private string _xText = "X: -";

	/// <summary>
	///     Gets or sets the formatted Y coordinate text (e.g., "Y: 67.89").
	/// </summary>
	[ObservableProperty] private string _yText = "Y: -";

	/// <summary>
	///     Gets or sets the formatted Z coordinate text (e.g., "Z: 10.11").
	/// </summary>
	[ObservableProperty] private string _zText = "Z: -";

	/// <summary>
	///     Initializes a new instance of the <see cref="StatusBarViewModel" /> class.
	/// </summary>
	/// <param name="mumbleService">The Mumble service providing Guild Wars 2 player state.</param>
	/// <param name="dispatcherHelper">The dispatcher helper for UI thread synchronization.</param>
	/// <param name="updateChecker">The update checker service for version checking.</param>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="mumbleService" />, <paramref name="dispatcherHelper" />,
	///     <paramref name="updateChecker" />, or <paramref name="logger" /> is <see langword="null" />.
	/// </exception>
	public StatusBarViewModel(
		IMumbleService mumbleService,
		IDispatcherHelper dispatcherHelper,
		IUpdateCheckerService updateChecker,
		IGw2DataCacheService gw2DataCache,
		INotificationService notificationService,
		ILogger<StatusBarViewModel> logger)
	{
		ArgumentNullException.ThrowIfNull(mumbleService);
		ArgumentNullException.ThrowIfNull(dispatcherHelper);
		ArgumentNullException.ThrowIfNull(updateChecker);
		ArgumentNullException.ThrowIfNull(gw2DataCache);
		ArgumentNullException.ThrowIfNull(notificationService);
		ArgumentNullException.ThrowIfNull(logger);

		_mumbleService = mumbleService;
		_dispatcherHelper = dispatcherHelper;
		_updateChecker = updateChecker;
		_gw2DataCache = gw2DataCache;
		_notificationService = notificationService;
		_logger = logger;

		_mumbleService.MumbleUpdated += OnMumbleUpdated;

		// Subscribe to update checker property changes to forward notifications
		if (_updateChecker is INotifyPropertyChanged notifyPropertyChanged)
			notifyPropertyChanged.PropertyChanged += OnUpdateCheckerPropertyChanged;

		_logger.LogDebug("StatusBarViewModel initialized and subscribed to MumbleUpdated event.");
	}

	/// <summary>
	///     Disposes resources and unsubscribes from Mumble service events.
	/// </summary>
	public void Dispose()
	{
		_logger.LogDebug("Disposing StatusBarViewModel.");

		_mumbleService.MumbleUpdated -= OnMumbleUpdated;

		// Unsubscribe from update checker property changes
		if (_updateChecker is INotifyPropertyChanged notifyPropertyChanged)
			notifyPropertyChanged.PropertyChanged -= OnUpdateCheckerPropertyChanged;

		_logger.LogInformation("StatusBarViewModel disposed successfully.");
	}

	/// <summary>
	///     Gets a value indicating whether an application update is available.
	/// </summary>
	public bool IsUpdateAvailable => _updateChecker.IsUpdateAvailable;

	/// <summary>
	///     Gets the URL of the latest release on GitHub.
	/// </summary>
	public string? LatestVersionUrl => _updateChecker.LatestVersionUrl;

	/// <summary>
	///     Gets the tag name of the latest version (e.g., "v1.2.3").
	/// </summary>
	public string? LatestVersionTag => _updateChecker.LatestVersionTag;

	/// <summary>
	///     Opens the KXTools website in the default browser.
	/// </summary>
	[RelayCommand]
	private void OpenWebsite()
	{
		OpenUrl(Constants.Settings.KxToolsWebsiteUrl, "KXTools website");
	}

	/// <summary>
	///     Opens the Discord invite link in the default browser.
	/// </summary>
	[RelayCommand]
	private void OpenDiscord()
	{
		OpenUrl(Constants.Settings.DiscordInviteUrl, "Discord invite");
	}

	/// <summary>
	///     Opens the GitHub repository in the default browser.
	/// </summary>
	[RelayCommand]
	private void OpenGitHub()
	{
		OpenUrl(Constants.Settings.GitHubRepoUrl, "GitHub repository");
	}

	/// <summary>
	///     Opens the latest release page on GitHub in the default browser.
	/// </summary>
	[RelayCommand]
	private void OpenLatestRelease()
	{
		if (string.IsNullOrEmpty(LatestVersionUrl))
		{
			_logger.LogWarning("Attempted to open latest release URL, but URL is null or empty.");
			return;
		}

		OpenUrl(LatestVersionUrl, "latest release");
	}

	/// <summary>
	///     Downloads the latest GW2 data files (maps.json and continents.json) from the API.
	/// </summary>
	[RelayCommand]
	private async Task DownloadGw2DataAsync()
	{
		_logger.LogInformation("Starting GW2 data download");

		try
		{
			// Download both files
			await _gw2DataCache.DownloadMapsAsync();
			await _gw2DataCache.DownloadContinentsAsync();

			// Invalidate cache so next access loads the new files
			_gw2DataCache.InvalidateCache();

			_logger.LogInformation("GW2 data download completed successfully");
			_notificationService.ShowSuccess("GW2 data files downloaded successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to download GW2 data files");
			_notificationService.ShowWarning("Failed to download GW2 data files. Check your network connection.");
		}
	}

	/// <summary>
	///     Opens a URL in the default browser with error handling and logging.
	/// </summary>
	/// <param name="url">The URL to open.</param>
	/// <param name="description">A human-readable description for logging (e.g., "website", "Discord invite").</param>
	private void OpenUrl(string url, string description)
	{
		try
		{
			_logger.LogInformation("Opening {Description}: {Url}", description, url);
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to open {Description}: {Url}", description, url);
		}
	}

	/// <summary>
	///     Handles Mumble state updates and synchronizes UI properties on the dispatcher thread.
	/// </summary>
	/// <param name="sender">The event source (typically <see cref="IMumbleService" />).</param>
	/// <param name="mumble">The updated Mumble state containing player position and connection status.</param>
	private void OnMumbleUpdated(object? sender, MumbleStateModel mumble)
	{
		_dispatcherHelper.InvokeOnUIThread(() =>
		{
			ConnectionState = mumble.ConnectionState;

			if (mumble.ConnectionState == MumbleConnectionState.Disconnected)
			{
				_logger.LogDebug("Mumble disconnected. Resetting status bar UI.");

				CharacterName = "Not connected";
				MapText = "Map: N/A";
				CoordinatesText = "Pos: N/A";
				XText = "X: -";
				YText = "Y: -";
				ZText = "Z: -";
				return;
			}

			// Update character name with AFK indicator if stale
			CharacterName = mumble.ConnectionState == MumbleConnectionState.Stale
				? string.IsNullOrWhiteSpace(mumble.CharacterName) ? "Unknown" : $"{mumble.CharacterName} (AFK)"
				: string.IsNullOrWhiteSpace(mumble.CharacterName)
					? "Unknown"
					: mumble.CharacterName;

			MapText = $"Map: {mumble.CurrentMapId}";

			var x = mumble.PlayerPosition.X;
			var y = mumble.PlayerPosition.Y;
			var z = mumble.PlayerPosition.Z;

			XText = $"X: {x:0.##}";
			YText = $"Y: {y:0.##}";
			ZText = $"Z: {z:0.##}";
			CoordinatesText = $"Pos: {x:0.##}, {y:0.##}, {z:0.##}";

			_logger.LogTrace(
				"Status bar updated: Character={Character}, Map={MapId}, Position=({X:0.##}, {Y:0.##}, {Z:0.##})",
				CharacterName, mumble.CurrentMapId, x, y, z);
		});
	}

	/// <summary>
	///     Handles property changes from the update checker service to forward notifications to the UI.
	/// </summary>
	/// <param name="sender">The event source (typically <see cref="IUpdateCheckerService" />).</param>
	/// <param name="e">The property change event arguments.</param>
	private void OnUpdateCheckerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		_dispatcherHelper.InvokeOnUIThread(() =>
		{
			// Forward property change notifications for update checker properties
			if (e.PropertyName == nameof(IUpdateCheckerService.IsUpdateAvailable))
				OnPropertyChanged(nameof(IsUpdateAvailable));
			else if (e.PropertyName == nameof(IUpdateCheckerService.LatestVersionUrl))
				OnPropertyChanged(nameof(LatestVersionUrl));
			else if (e.PropertyName == nameof(IUpdateCheckerService.LatestVersionTag))
				OnPropertyChanged(nameof(LatestVersionTag));
		});
	}
}