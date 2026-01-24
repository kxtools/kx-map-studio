namespace KXMapStudio.Core.Abstractions.Services;

/// <summary>
///     Defines lazy-loading access to cached Guild Wars 2 API data (maps and continents).
/// </summary>
/// <remarks>
///     This service provides on-demand loading of GW2 data from local JSON files,
///     with caching to avoid repeated file I/O operations.
/// </remarks>
public interface IGw2DataCacheService
{
	/// <summary>
	///     Gets the cached maps data, loading from disk if not already cached.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A read-only list of maps.</returns>
	/// <exception cref="FileNotFoundException">Thrown when maps.json file is not found.</exception>
	/// <exception cref="JsonException">Thrown when the JSON file is corrupted or invalid.</exception>
	Task<IReadOnlyList<MapModel>> GetMapsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Gets the cached continents data, loading from disk if not already cached.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The continent floor model.</returns>
	/// <exception cref="FileNotFoundException">Thrown when continents.json file is not found.</exception>
	/// <exception cref="JsonException">Thrown when the JSON file is corrupted or invalid.</exception>
	Task<ContinentFloorModel> GetContinentsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Invalidates all cached data, forcing the next Get operation to reload from disk.
	/// </summary>
	/// <remarks>
	///     Use this after downloading new GW2 data files to ensure fresh data is loaded.
	/// </remarks>
	void InvalidateCache();

	/// <summary>
	///     Downloads the latest maps data from GW2 API and saves it to disk.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
	/// <exception cref="IOException">Thrown when file write fails.</exception>
	Task DownloadMapsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Downloads the latest continents data from GW2 API and saves it to disk.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
	/// <exception cref="IOException">Thrown when file write fails.</exception>
	Task DownloadContinentsAsync(CancellationToken cancellationToken = default);
}