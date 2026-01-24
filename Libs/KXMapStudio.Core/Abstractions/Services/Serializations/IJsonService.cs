namespace KXMapStudio.Core.Abstractions.Services.Serializations;

/// <summary>
///     Defines comprehensive JSON-oriented workflows used by KXMapStudio.
/// </summary>
/// <remarks>
///     This service provides business-level JSON operations including:
///     <list type="bullet">
///         <item>
///             <description>KX JSON model CRUD operations</description>
///         </item>
///         <item>
///             <description>Guild Wars 2 API payload caching</description>
///         </item>
///         <item>
///             <description>Binary (byte[]) serialization for performance-critical scenarios</description>
///         </item>
///         <item>
///             <description>Generic JSON persistence for extensibility</description>
///         </item>
///     </list>
/// </remarks>
public interface IJsonService
{
	// ====== KX JSON Model Operations ======

	/// <summary>
	///     Loads a KX JSON model from the specified file.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the parsed model.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="InvalidDataException">Thrown when the JSON is missing or invalid.</exception>
	Task<JsonModel> LoadAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	///     Saves a KX JSON model to the specified file.
	/// </summary>
	/// <param name="data">The model to serialize. Must not be <see langword="null" />.</param>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	Task SaveAsync(JsonModel data, string path, CancellationToken cancellationToken = default);

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
	Task<JsonModel> CreateNewAsync(
		string name,
		string? author,
		CoordinatesModel[] coordinates,
		string outputPath,
		CancellationToken cancellationToken = default);

	// ====== Guild Wars 2 API Payload Operations ======

	/// <summary>
	///     Loads the cached Guild Wars 2 maps payload from the specified file.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains a read-only list of maps.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     Returns an empty list when the file is missing or invalid, allowing graceful degradation.
	/// </remarks>
	Task<IReadOnlyList<MapModel>> LoadGuildWarsMapsAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	///     Loads the cached Guild Wars 2 continent floor payload from the specified file.
	/// </summary>
	/// <param name="path">The JSON file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the parsed payload, or <see langword="null" /> when the file is missing or invalid.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	Task<ContinentFloorModel?> LoadGuildWarsContinentFloorAsync(
		string path,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Saves Guild Wars 2 maps data to the specified file.
	/// </summary>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="maps">The maps data to save. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="maps" /> is <see langword="null" />.</exception>
	Task SaveGuildWarsMapsAsync(
		string path,
		IReadOnlyList<MapModel> maps,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Saves Guild Wars 2 continent floor data to the specified file.
	/// </summary>
	/// <param name="path">The output file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="continentFloor">The continent floor data to save. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="continentFloor" /> is <see langword="null" />.</exception>
	Task SaveGuildWarsContinentFloorAsync(
		string path,
		ContinentFloorModel continentFloor,
		CancellationToken cancellationToken = default);

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
	///     Enables high-performance in-memory serialization for caching, network transmission,
	///     or inter-process communication scenarios.
	/// </remarks>
	Task<byte[]> SerializeModelToBytesAsync(JsonModel data, CancellationToken cancellationToken = default);

	/// <summary>
	///     Deserializes a KX JSON model from a UTF-8 encoded byte array.
	/// </summary>
	/// <param name="jsonBytes">The UTF-8 encoded JSON byte array. Must not be <see langword="null" /> or empty.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the deserialized model, or <see langword="null" /> when the payload is invalid.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonBytes" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="jsonBytes" /> is empty.</exception>
	Task<JsonModel?> DeserializeModelFromBytesAsync(byte[] jsonBytes, CancellationToken cancellationToken = default);
}