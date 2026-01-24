namespace KXMapStudio.Core.Abstractions.Repositories.Serializations;

/// <summary>
///     Defines a high-performance persistence boundary for JSON files with support
///     for both file-based and in-memory (byte[]) operations.
/// </summary>
/// <remarks>
///     Implementations MUST use streaming-first APIs and async patterns to ensure
///     optimal performance and resource utilization. The repository supports:
///     <list type="bullet">
///         <item>
///             <description>File-based serialization/deserialization</description>
///         </item>
///         <item>
///             <description>In-memory binary (byte[]) operations for caching and network scenarios</description>
///         </item>
///         <item>
///             <description>Comprehensive logging for all operations</description>
///         </item>
///     </list>
/// </remarks>
public interface IJsonRepository
{
	// ====== File-Based Operations ======

	/// <summary>
	///     Serializes and saves the specified data to the specified file path.
	/// </summary>
	/// <typeparam name="T">The type of data to serialize.</typeparam>
	/// <param name="data">The data instance to serialize. Must not be <see langword="null" />.</param>
	/// <param name="outputFile">The destination file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>A <see cref="Task" /> that represents the asynchronous save operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="outputFile" /> is <see langword="null" /> or
	///     whitespace.
	/// </exception>
	Task SaveAsync<T>(T data, string outputFile, CancellationToken cancellationToken = default);

	/// <summary>
	///     Loads and deserializes JSON content from the specified file path.
	/// </summary>
	/// <typeparam name="T">The target model type to deserialize into.</typeparam>
	/// <param name="path">The source file path. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains the deserialized instance, or <see langword="default" />
	///     when the file is missing or contains invalid JSON.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is <see langword="null" /> or whitespace.</exception>
	/// <remarks>
	///     Implementations SHOULD be tolerant and return <see langword="default" /> for malformed JSON
	///     instead of throwing exceptions. This allows graceful degradation in production scenarios.
	/// </remarks>
	Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default);

	// ====== Binary Operations (Byte Array) ======

	/// <summary>
	///     Serializes the specified object to a UTF-8 encoded JSON byte array.
	/// </summary>
	/// <typeparam name="T">The type of the object to serialize.</typeparam>
	/// <param name="data">The data instance to serialize. Must not be <see langword="null" />.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>
	///     A <see cref="Task{TResult}" /> that represents the asynchronous operation.
	///     The task result contains a byte array with the UTF-8 encoded JSON representation.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="data" /> is <see langword="null" />.</exception>
	/// <remarks>
	///     This method provides high-performance in-memory serialization without file system access.
	///     Ideal for:
	///     <list type="bullet">
	///         <item>
	///             <description>Caching scenarios</description>
	///         </item>
	///         <item>
	///             <description>HTTP response bodies</description>
	///         </item>
	///         <item>
	///             <description>Inter-process communication</description>
	///         </item>
	///         <item>
	///             <description>Network transmission</description>
	///         </item>
	///     </list>
	/// </remarks>
	Task<byte[]> SerializeToBytesAsync<T>(T data, CancellationToken cancellationToken = default);

	/// <summary>
	///     Deserializes a UTF-8 encoded JSON byte array into the specified type.
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
	///     This method enables high-performance deserialization from in-memory byte arrays.
	///     Useful for:
	///     <list type="bullet">
	///         <item>
	///             <description>Network payloads</description>
	///         </item>
	///         <item>
	///             <description>Embedded resources</description>
	///         </item>
	///         <item>
	///             <description>Cached data</description>
	///         </item>
	///         <item>
	///             <description>Memory-mapped files</description>
	///         </item>
	///     </list>
	/// </remarks>
	Task<T?> DeserializeFromBytesAsync<T>(byte[] jsonBytes, CancellationToken cancellationToken = default);
}