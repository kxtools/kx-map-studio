namespace KXMapStudio.Core.Repositories.FileStorage;

/// <summary>
///     Provides a minimal file system storage implementation for Core repositories.
/// </summary>
/// <remarks>
///     This repository is intentionally small and focused: higher-level repositories handle format concerns
///     (JSON/XML/ZIP).
/// </remarks>
internal sealed record FileStorageRepository : IFileStorageRepository
{
	/// <inheritdoc />
	public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(content);

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var options = new FileStreamOptions
		{
			Mode = FileMode.Create,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
			BufferSize = Constants.Settings.DefaultBufferSize
		};

		await using var fileStream = new FileStream(path, options);
		await content.CopyToAsync(fileStream, cancellationToken);
	}

	/// <inheritdoc />
	public Task<Stream?> LoadAsync(string path, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		if (!Exists(path))
			return Task.FromResult<Stream?>(null);

		var options = new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
			BufferSize = Constants.Settings.DefaultBufferSize
		};

		return Task.FromResult<Stream?>(new FileStream(path, options));
	}

	/// <inheritdoc />
	public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return Task.Run(() =>
		{
			if (File.Exists(path)) File.Delete(path);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public bool Exists(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return File.Exists(path);
	}
}