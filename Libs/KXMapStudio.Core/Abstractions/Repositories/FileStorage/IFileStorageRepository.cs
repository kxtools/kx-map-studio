namespace KXMapStudio.Core.Abstractions.Repositories.FileStorage;

/// <summary>
///     Defines a minimal file storage boundary used by Core repositories.
/// </summary>
/// <remarks>
///     Implementations should favor asynchronous, streaming-first I/O.
/// </remarks>
public interface IFileStorageRepository
{
	/// <summary>
	///     Saves the provided content stream to the specified path.
	/// </summary>
	/// <param name="path">The destination file path.</param>
	/// <param name="content">The content stream to persist.</param>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

	/// <summary>
	///     Loads the specified file as a readable stream.
	/// </summary>
	/// <param name="path">The source file path.</param>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>A readable stream, or <see langword="null" /> when the file does not exist.</returns>
	Task<Stream?> LoadAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	///     Deletes a file from the file system.
	/// </summary>
	/// <param name="path">Full path to the file to delete.</param>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	///     Determines whether a file exists at the specified path.
	/// </summary>
	/// <param name="path">The file path.</param>
	/// <returns><see langword="true" /> if the file exists; otherwise, <see langword="false" />.</returns>
	bool Exists(string path);
}