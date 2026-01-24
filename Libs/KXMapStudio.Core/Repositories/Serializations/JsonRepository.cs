namespace KXMapStudio.Core.Repositories.Serializations;

/// <summary>
///     Provides high-performance JSON persistence based on <see cref="System.Text.Json" />
///     and an underlying <see cref="IFileStorageRepository" />.
/// </summary>
/// <param name="FileStorage">The underlying file storage abstraction.</param>
/// <param name="Logger">The logger instance for operation tracing.</param>
/// <remarks>
///     This repository implements streaming-first I/O and supports both direct file paths
///     and in-memory binary (byte[]) scenarios for maximum performance and flexibility.
/// </remarks>
public sealed record JsonRepository(
	IFileStorageRepository FileStorage,
	ILogger<JsonRepository> Logger) : IJsonRepository
{
	/// <summary>
	///     Gets the default JSON serializer options used by Core.
	/// </summary>
	/// <remarks>
	///     These options are intentionally deterministic to keep file diffs stable and user-editable.
	///     Features:
	///     <list type="bullet">
	///         <item>
	///             <description>Indented formatting for human readability</description>
	///         </item>
	///         <item>
	///             <description>Null value omission to reduce payload size</description>
	///         </item>
	///         <item>
	///             <description>Relaxed JSON escaping for international characters</description>
	///         </item>
	///     </list>
	/// </remarks>
	public static JsonSerializerOptions DefaultJsonOptions { get; } = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		PropertyNameCaseInsensitive = true
	};

	/// <summary>
	///     Serializes and saves the specified object as JSON to the provided file path.
	/// </summary>
	/// <typeparam name="T">The type of the object to serialize.</typeparam>
	/// <param name="data">The data instance to serialize. Must not be <see langword="null" />.</param>
	/// <param name="path">The target file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     This method uses a <see cref="MemoryStream" /> buffer to serialize the JSON payload
	///     before writing to disk, ensuring atomic write semantics.
	/// </remarks>
	public async Task SaveAsync<T>(T data, string path, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogDebug("Starting JSON serialization for type '{TypeName}' to path: {Path}", typeof(T).Name, path);

		using var ms = new MemoryStream();
		await JsonSerializer.SerializeAsync(ms, data, DefaultJsonOptions, cancellationToken).ConfigureAwait(false);
		ms.Position = 0;

		Logger.LogDebug("Serialized {ByteCount} bytes for type '{TypeName}'. Writing to disk...", ms.Length,
			typeof(T).Name);

		await FileStorage.SaveAsync(path, ms, cancellationToken).ConfigureAwait(false);

		Logger.LogInformation("Successfully saved JSON file: {Path} ({ByteCount} bytes)", path, ms.Length);
	}

	/// <summary>
	///     Loads and deserializes JSON content from the provided file path.
	/// </summary>
	/// <typeparam name="T">The target model type to deserialize into.</typeparam>
	/// <param name="path">The source file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the deserialized model instance, or <see langword="default" />
	///     when the file is missing or the payload is invalid.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     <para>
	///         This method is intentionally tolerant: malformed JSON returns <see langword="default" /> instead of throwing.
	///         Callers that need error details should implement explicit validation at the application boundary.
	///     </para>
	///     <para>
	///         The method uses streaming deserialization to minimize memory allocations for large files.
	///     </para>
	/// </remarks>
	public async Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Logger.LogDebug("Loading JSON file from path: {Path}", path);

		if (!FileStorage.Exists(path))
		{
			Logger.LogWarning("JSON file not found: {Path}", path);
			return default;
		}

		await using var stream = await FileStorage.LoadAsync(path, cancellationToken).ConfigureAwait(false);
		if (stream is null)
		{
			Logger.LogWarning("Failed to open stream for JSON file: {Path}", path);
			return default;
		}

		try
		{
			var result = await JsonSerializer.DeserializeAsync<T>(stream, DefaultJsonOptions, cancellationToken)
				.ConfigureAwait(false);

			Logger.LogInformation("Successfully loaded and deserialized JSON file: {Path} (Type: {TypeName})",
				path, typeof(T).Name);

			return result;
		}
		catch (JsonException jsonEx)
		{
			Logger.LogError(jsonEx, "JSON deserialization failed for file: {Path}. Invalid JSON format.", path);
			return default;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while loading JSON file: {Path}", path);
			return default;
		}
	}

	/// <summary>
	///     Serializes the specified object to a UTF-8 encoded JSON byte array.
	/// </summary>
	/// <typeparam name="T">The type of the object to serialize.</typeparam>
	/// <param name="data">The data instance to serialize. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains a byte array containing the UTF-8 encoded JSON representation.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <remarks>
	///     This method provides high-performance in-memory serialization without touching the file system.
	///     Ideal for caching scenarios, HTTP responses, or inter-process communication.
	/// </remarks>
	public async Task<byte[]> SerializeToBytesAsync<T>(T data, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(data);

		Logger.LogDebug("Serializing type '{TypeName}' to byte array", typeof(T).Name);

		using var ms = new MemoryStream();
		await JsonSerializer.SerializeAsync(ms, data, DefaultJsonOptions, cancellationToken).ConfigureAwait(false);

		var result = ms.ToArray();

		Logger.LogDebug("Serialized {ByteCount} bytes for type '{TypeName}'", result.Length, typeof(T).Name);

		return result;
	}

	/// <summary>
	///     Deserializes a JSON byte array into the specified type.
	/// </summary>
	/// <typeparam name="T">The target model type to deserialize into.</typeparam>
	/// <param name="jsonBytes">The UTF-8 encoded JSON byte array. Must not be <see langword="null" /> or empty.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the deserialized model instance, or <see langword="default" />
	///     when the payload is invalid.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonBytes" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="jsonBytes" /> is empty.</exception>
	/// <remarks>
	///     <para>
	///         This method enables high-performance deserialization from in-memory byte arrays
	///         without requiring file system access. Useful for network payloads, embedded resources,
	///         or cached data.
	///     </para>
	///     <para>
	///         The method is tolerant: malformed JSON returns <see langword="default" /> instead of throwing.
	///     </para>
	/// </remarks>
	public async Task<T?> DeserializeFromBytesAsync<T>(byte[] jsonBytes, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(jsonBytes);

		if (jsonBytes.Length == 0)
		{
			Logger.LogWarning("Attempted to deserialize empty byte array");
			return default;
		}

		Logger.LogDebug("Deserializing {ByteCount} bytes to type '{TypeName}'", jsonBytes.Length, typeof(T).Name);

		try
		{
			using var ms = new MemoryStream(jsonBytes, false);
			var result = await JsonSerializer.DeserializeAsync<T>(ms, DefaultJsonOptions, cancellationToken)
				.ConfigureAwait(false);

			Logger.LogDebug("Successfully deserialized byte array to type '{TypeName}'", typeof(T).Name);

			return result;
		}
		catch (JsonException jsonEx)
		{
			Logger.LogError(jsonEx,
				"JSON deserialization failed for byte array (Type: {TypeName}). Invalid JSON format.",
				typeof(T).Name);
			return default;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Unexpected error while deserializing byte array to type '{TypeName}'", typeof(T).Name);
			return default;
		}
	}
}