namespace KXMapStudio.Core.Services;

/// <summary>
///     Provides lazy-loading access to cached Guild Wars 2 API data with download capabilities.
/// </summary>
/// <remarks>
///     This service implements a simple in-memory cache for GW2 maps and continents data.
///     Data is loaded from local JSON files on first access and can be invalidated after updates.
/// </remarks>
public sealed class Gw2DataCacheService : IGw2DataCacheService
{
	private readonly SemaphoreSlim _continentsLock = new(1, 1);
	private readonly string _dataDirectory;
	private readonly IGuildWarsHttpClient _httpClient;
	private readonly IJsonService _jsonService;
	private readonly ILogger<Gw2DataCacheService> _logger;
	private readonly SemaphoreSlim _mapsLock = new(1, 1);
	private ContinentFloorModel? _cachedContinents;

	private IReadOnlyList<MapModel>? _cachedMaps;

	/// <summary>
	///     Initializes a new instance of the <see cref="Gw2DataCacheService" /> class.
	/// </summary>
	/// <param name="httpClient">The HTTP client for downloading GW2 data.</param>
	/// <param name="jsonService">The JSON service for reading local files.</param>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when any constructor parameter is <see langword="null" />.
	/// </exception>
	public Gw2DataCacheService(
		IGuildWarsHttpClient httpClient,
		IJsonService jsonService,
		ILogger<Gw2DataCacheService> logger)
	{
		ArgumentNullException.ThrowIfNull(httpClient);
		ArgumentNullException.ThrowIfNull(jsonService);
		ArgumentNullException.ThrowIfNull(logger);

		_httpClient = httpClient;
		_jsonService = jsonService;
		_logger = logger;

		// Data files are stored directly in the executable directory
		_dataDirectory = AppContext.BaseDirectory;
	}

	/// <summary>
	///     Gets the cached maps data, loading from disk if not already cached.
	/// </summary>
	public async Task<IReadOnlyList<MapModel>> GetMapsAsync(CancellationToken cancellationToken = default)
	{
		if (_cachedMaps != null)
			return _cachedMaps;

		await _mapsLock.WaitAsync(cancellationToken);
		try
		{
			// Double-check after acquiring lock
			if (_cachedMaps != null)
				return _cachedMaps;

			var mapsPath = Path.Combine(_dataDirectory, Constants.Settings.MapsPath);

			if (!File.Exists(mapsPath))
			{
				_logger.LogWarning("Maps file not found: {MapsPath}", mapsPath);
				throw new FileNotFoundException($"Maps data file not found: {mapsPath}");
			}

			_logger.LogInformation("Loading maps data from: {MapsPath}", mapsPath);
			_cachedMaps = await _jsonService.LoadGuildWarsMapsAsync(mapsPath, cancellationToken);
			_logger.LogInformation("Loaded {Count} maps from cache", _cachedMaps.Count);

			return _cachedMaps;
		}
		finally
		{
			_mapsLock.Release();
		}
	}

	/// <summary>
	///     Gets the cached continents data, loading from disk if not already cached.
	/// </summary>
	public async Task<ContinentFloorModel> GetContinentsAsync(CancellationToken cancellationToken = default)
	{
		if (_cachedContinents != null)
			return _cachedContinents;

		await _continentsLock.WaitAsync(cancellationToken);
		try
		{
			// Double-check after acquiring lock
			if (_cachedContinents != null)
				return _cachedContinents;

			var continentsPath = Path.Combine(_dataDirectory, Constants.Settings.ContinentsPath);

			if (!File.Exists(continentsPath))
			{
				_logger.LogWarning("Continents file not found: {ContinentsPath}", continentsPath);
				throw new FileNotFoundException($"Continents data file not found: {continentsPath}");
			}

			_logger.LogInformation("Loading continents data from: {ContinentsPath}", continentsPath);
			_cachedContinents = await _jsonService.LoadGuildWarsContinentFloorAsync(continentsPath, cancellationToken);
			_logger.LogInformation("Loaded continents data from cache");

			return _cachedContinents!;
		}
		finally
		{
			_continentsLock.Release();
		}
	}

	/// <summary>
	///     Invalidates all cached data, forcing the next Get operation to reload from disk.
	/// </summary>
	public void InvalidateCache()
	{
		_logger.LogInformation("Invalidating GW2 data cache");
		_cachedMaps = null;
		_cachedContinents = null;
	}

	/// <summary>
	///     Downloads the latest maps data from GW2 API and saves it to disk.
	/// </summary>
	public async Task DownloadMapsAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Downloading maps data from GW2 API");

		var maps = await _httpClient.GetMapsAsync(cancellationToken);

		var mapsPath = Path.Combine(_dataDirectory, Constants.Settings.MapsPath);
		await _jsonService.SaveGuildWarsMapsAsync(mapsPath, maps, cancellationToken);

		_logger.LogInformation("Maps data saved to: {MapsPath}", mapsPath);
	}

	/// <summary>
	///     Downloads the latest continents data from GW2 API and saves it to disk.
	/// </summary>
	public async Task DownloadContinentsAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Downloading continents data from GW2 API");

		var continents = await _httpClient.GetContinentsAsync(cancellationToken);

		var continentsPath = Path.Combine(_dataDirectory, Constants.Settings.ContinentsPath);
		await _jsonService.SaveGuildWarsContinentFloorAsync(continentsPath, continents, cancellationToken);

		_logger.LogInformation("Continents data saved to: {ContinentsPath}", continentsPath);
	}
}