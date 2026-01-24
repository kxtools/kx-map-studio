namespace KXMapStudio.Core.Services.Serializations;

/// <summary>
///     Provides comprehensive JSON-oriented domain workflows on top of <see cref="IJsonRepository" />.
/// </summary>
/// <param name="JsonRepository">The JSON persistence repository.</param>
/// <param name="Logger">The logger instance for operation tracing.</param>
/// <remarks>
///     This service implements business-level JSON operations including:
///     <list type="bullet">
///         <item>
///             <description>KX JSON model persistence (read/write)</description>
///         </item>
///         <item>
///             <description>Guild Wars 2 API payload caching</description>
///         </item>
///         <item>
///             <description>Binary (byte[]) operations for performance-critical scenarios</description>
///         </item>
///         <item>
///             <description>New document creation with validation</description>
///         </item>
///     </list>
/// </remarks>
public sealed record JsonService(
	IJsonRepository JsonRepository,
	ILogger<JsonService> Logger) : IJsonService
{
	// ====== KX JSON Model Operations ======

	/// <summary>
	///     Loads the core KX JSON model from the specified file path.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the parsed <see cref="JsonModel" /> instance.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="InvalidDataException">Thrown when the JSON is missing or invalid.</exception>
	/// <remarks>
	///     This method enforces strict validation: missing or malformed JSON will throw an exception.
	///     For tolerant loading, use the underlying repository directly.
	/// </remarks>
	public async Task<JsonModel> LoadAsync(string path, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogInformation("Loading KX JSON model from: {Path}", path);

		var result = await JsonRepository.LoadAsync<JsonModel>(path, cancellationToken).ConfigureAwait(false);

		if (result is null)
		{
			Logger.LogError("Failed to load KX JSON model from '{Path}'. File missing or invalid.", path);
			throw new InvalidDataException($"Unable to load JSON model from '{path}'.");
		}

		Logger.LogInformation("Successfully loaded KX JSON model from '{Path}' with {CoordinateCount} coordinates",
			path, result.Coordinates.Length);

		return result;
	}

	/// <summary>
	///     Saves a core KX JSON model to the specified file path.
	/// </summary>
	/// <param name="data">The model to serialize. Must not be <see langword="null" />.</param>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	public async Task SaveAsync(JsonModel data, string path, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogInformation("Saving KX JSON model to: {Path} (Coordinates: {Count})", path, data.Coordinates.Length);

		await JsonRepository.SaveAsync(data, path, cancellationToken).ConfigureAwait(false);

		Logger.LogInformation("Successfully saved KX JSON model to: {Path}", path);
	}

	/// <summary>
	///     Creates a new KX JSON model with the specified parameters and saves it to disk.
	/// </summary>
	/// <param name="name">The document name. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="author">The optional author name. Can be <see langword="null" />.</param>
	/// <param name="coordinates">The array of coordinate entries. Must not be <see langword="null" />.</param>
	/// <param name="outputPath">The destination file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the newly created <see cref="JsonModel" /> instance.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="name" /> or <paramref name="outputPath" /> is
	///     <see langword="null" /> or whitespace.
	/// </exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="coordinates" /> is <see langword="null" />.</exception>
	/// <remarks>
	///     This method creates a new JSON model in memory and immediately persists it to disk.
	///     The returned model can be used for further operations without reloading from disk.
	/// </remarks>
	public async Task<JsonModel> CreateNewAsync(
		string name,
		string? author,
		CoordinatesModel[] coordinates,
		string outputPath,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(coordinates);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

		Logger.LogInformation(
			"Creating new KX JSON model: Name='{Name}', Author='{Author}', Coordinates={Count}, Output='{Path}'",
			name, author ?? "(none)", coordinates.Length, outputPath);

		var model = new JsonModel(
			name,
			author,
			coordinates
		);

		await SaveAsync(model, outputPath, cancellationToken).ConfigureAwait(false);

		Logger.LogInformation("Successfully created new KX JSON model at: {Path}", outputPath);

		return model;
	}

	// ====== Guild Wars 2 API Payload Operations ======

	/// <summary>
	///     Loads the cached Guild Wars 2 maps payload from the specified file.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains a read-only list of maps; returns an empty list when the file is missing or invalid.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     This method is tolerant: missing or malformed files return an empty list instead of throwing.
	///     This allows graceful degradation when cached API data is unavailable.
	/// </remarks>
	public async Task<IReadOnlyList<MapModel>> LoadGuildWarsMapsAsync(
		string path,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogDebug("Loading Guild Wars 2 maps from: {Path}", path);

		var data = await JsonRepository.LoadAsync<IReadOnlyList<MapModel>>(path, cancellationToken)
			.ConfigureAwait(false);

		if (data is null || data.Count == 0)
		{
			Logger.LogWarning("No Guild Wars 2 maps loaded from '{Path}'. Returning empty list.", path);
			return [];
		}

		Logger.LogInformation("Successfully loaded {Count} Guild Wars 2 maps from: {Path}", data.Count, path);

		return data;
	}

	/// <summary>
	///     Loads the cached Guild Wars 2 continent floor payload from the specified file.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the parsed model, or <see langword="null" /> when the file is missing or invalid.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     This method is tolerant: missing or malformed files return <see langword="null" /> instead of throwing.
	/// </remarks>
	public async Task<ContinentFloorModel?> LoadGuildWarsContinentFloorAsync(
		string path,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogDebug("Loading Guild Wars 2 continent floor data from: {Path}", path);

		var result = await JsonRepository.LoadAsync<ContinentFloorModel>(path, cancellationToken)
			.ConfigureAwait(false);

		if (result is null)
		{
			Logger.LogWarning("Failed to load Guild Wars 2 continent floor data from: {Path}", path);
			return null;
		}

		Logger.LogInformation(
			"Successfully loaded Guild Wars 2 continent floor data from '{Path}' (Floor ID: {FloorId}, Regions: {RegionCount})",
			path, result.Id, result.Regions.Count);

		return result;
	}

	/// <summary>
	///     Saves Guild Wars 2 maps data to the specified file.
	/// </summary>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="maps">The maps data to save. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="maps" /> is <see langword="null" />.</exception>
	public async Task SaveGuildWarsMapsAsync(
		string path,
		IReadOnlyList<MapModel> maps,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(maps);

		Logger.LogInformation("Saving Guild Wars 2 maps data to: {Path} (Count: {Count})", path, maps.Count);

		await JsonRepository.SaveAsync(maps, path, cancellationToken).ConfigureAwait(false);

		Logger.LogInformation("Successfully saved {Count} Guild Wars 2 maps to: {Path}", maps.Count, path);
	}

	/// <summary>
	///     Saves Guild Wars 2 continent floor data to the specified file.
	/// </summary>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="continentFloor">The continent floor data to save. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="continentFloor" /> is <see langword="null" />.</exception>
	public async Task SaveGuildWarsContinentFloorAsync(
		string path,
		ContinentFloorModel continentFloor,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(continentFloor);

		Logger.LogInformation(
			"Saving Guild Wars 2 continent floor data to: {Path} (Floor ID: {FloorId}, Regions: {RegionCount})",
			path, continentFloor.Id, continentFloor.Regions.Count);

		await JsonRepository.SaveAsync(continentFloor, path, cancellationToken).ConfigureAwait(false);

		Logger.LogInformation("Successfully saved Guild Wars 2 continent floor data to: {Path}", path);
	}

	// ====== Binary Operations ======

	/// <summary>
	///     Serializes a KX JSON model to a UTF-8 encoded byte array.
	/// </summary>
	/// <param name="data">The model to serialize. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains a byte array with the UTF-8 encoded JSON representation.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <remarks>
	///     This method enables high-performance in-memory serialization without file system access.
	///     Ideal for caching, network transmission, or inter-process communication scenarios.
	/// </remarks>
	public async Task<byte[]> SerializeModelToBytesAsync(
		JsonModel data,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(data);

		Logger.LogDebug("Serializing KX JSON model to byte array: Name='{Name}'", data.Name);

		var result = await JsonRepository.SerializeToBytesAsync(data, cancellationToken).ConfigureAwait(false);

		Logger.LogDebug("Serialized KX JSON model '{Name}' to {ByteCount} bytes", data.Name, result.Length);

		return result;
	}

	/// <summary>
	///     Deserializes a KX JSON model from a UTF-8 encoded byte array.
	/// </summary>
	/// <param name="jsonBytes">The UTF-8 encoded JSON byte array. Must not be <see langword="null" /> or empty.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the deserialized <see cref="JsonModel" /> instance,
	///     or <see langword="null" /> when the payload is invalid.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonBytes" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="jsonBytes" /> is empty.</exception>
	/// <remarks>
	///     This method is tolerant: malformed JSON returns <see langword="null" /> instead of throwing.
	///     Useful for scenarios where JSON is received from external sources (network, cache, etc.).
	/// </remarks>
	public async Task<JsonModel?> DeserializeModelFromBytesAsync(
		byte[] jsonBytes,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(jsonBytes);

		Logger.LogDebug("Deserializing KX JSON model from {ByteCount} bytes", jsonBytes.Length);

		var result = await JsonRepository.DeserializeFromBytesAsync<JsonModel>(jsonBytes, cancellationToken)
			.ConfigureAwait(false);

		if (result is not null)
			Logger.LogDebug("Successfully deserialized KX JSON model: Name='{Name}', Coordinates={Count}",
				result.Name, result.Coordinates.Length);
		else
			Logger.LogWarning("Failed to deserialize KX JSON model from byte array");

		return result;
	}
}