namespace KXMapStudio.Libs.Services.GridEditor;

/// <summary>
///     Service responsible for loading and saving grid editor documents (JSON coordinates files).
/// </summary>
/// <param name="saveFileDialogService">The dialog service for file save operations.</param>
/// <param name="jsonService">The core JSON serialization service.</param>
/// <param name="logger">The logger for diagnostic and error tracking.</param>
/// <remarks>
///     <para>
///         This service handles both workspace files (direct file system access) and archive entries (ZIP/TACO embedded
///         files).
///     </para>
///     <para>
///         Archive entries support read-only access with "Save As" export functionality.
///     </para>
/// </remarks>
/// <exception cref="ArgumentNullException">
///     Thrown when any constructor parameter is <see langword="null" />.
/// </exception>
public sealed class GridEditorDocumentService(
	ISaveFileDialogService saveFileDialogService,
	IJsonService jsonService,
	ILogger<GridEditorDocumentService> logger)
	: IGridEditorDocumentService
{
	private readonly IJsonService _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));

	private readonly ILogger<GridEditorDocumentService> _logger =
		logger ?? throw new ArgumentNullException(nameof(logger));

	private readonly ISaveFileDialogService _saveFileDialogService = saveFileDialogService ??
	                                                                 throw new ArgumentNullException(
		                                                                 nameof(saveFileDialogService));

	/// <summary>
	///     Asynchronously loads a document and converts it to editable grid rows.
	/// </summary>
	/// <param name="doc">The document reference to load.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>
	///     A tuple containing the loaded rows and original file bytes (for dirty comparison).
	///     Returns empty collections if the document type is unsupported.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="doc" /> is <see langword="null" />.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	public async Task<(IReadOnlyList<GridRowData> rows, byte[] originalBytes)> LoadAsync(
		EditorDocumentReference doc,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(doc);

		_logger.LogInformation("Loading document: {DisplayName} (Extension: {Extension}, IsArchive: {IsArchive})",
			doc.DisplayName, doc.Extension, doc.IsArchiveEntry);

		var result = doc.Extension.ToLowerInvariant() switch
		{
			".json" => await LoadJsonAsync(doc, cancellationToken).ConfigureAwait(false),
			_ => ([], [])
		};

		_logger.LogInformation("Document loaded successfully: {DisplayName}. Row count: {RowCount}",
			doc.DisplayName, result.rows.Count);

		return result;
	}

	/// <summary>
	///     Asynchronously saves a document to its original location.
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="rows">The rows to save.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="doc" /> or <paramref name="rows" /> is <see langword="null" />.
	/// </exception>
	/// <exception cref="InvalidOperationException">Thrown when attempting to save an archive entry.</exception>
	/// <exception cref="NotSupportedException">Thrown when the file type is unsupported.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	public async Task SaveAsync(
		EditorDocumentReference doc,
		IReadOnlyList<GridRowData> rows,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(doc);
		ArgumentNullException.ThrowIfNull(rows);

		if (doc.IsArchiveEntry)
		{
			_logger.LogError("Attempted to save an archive entry directly: {DisplayName}", doc.DisplayName);
			throw new InvalidOperationException("Save is not supported for archive entries. Use Save As instead.");
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(doc.FilePath);

		_logger.LogInformation("Saving document: {FilePath} ({RowCount} rows)", doc.FilePath, rows.Count);

		switch (doc.Extension.ToLowerInvariant())
		{
			case ".json":
				await SaveJsonToPathAsync(rows, doc.FilePath!, _jsonService, _logger, cancellationToken)
					.ConfigureAwait(false);
				_logger.LogInformation("Document saved successfully: {FilePath}", doc.FilePath);
				break;
			default:
				_logger.LogError("Unsupported file type for Save: {Extension}", doc.Extension);
				throw new NotSupportedException($"Unsupported file type '{doc.Extension}'.");
		}
	}

	/// <summary>
	///     Asynchronously saves a document to a user-specified location (Save As).
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="rows">The rows to save.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="doc" /> or <paramref name="rows" /> is <see langword="null" />.
	/// </exception>
	/// <exception cref="NotSupportedException">Thrown when the file type is unsupported.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	public async Task SaveAsAsync(
		EditorDocumentReference doc,
		IReadOnlyList<GridRowData> rows,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(doc);
		ArgumentNullException.ThrowIfNull(rows);

		_logger.LogInformation("Initiating Save As for document: {DisplayName}", doc.DisplayName);

		switch (doc.Extension.ToLowerInvariant())
		{
			case ".json":
			{
				var targetPath = await _saveFileDialogService.ShowSaveJsonAsync(doc.DisplayName, cancellationToken)
					.ConfigureAwait(false);

				if (string.IsNullOrWhiteSpace(targetPath))
				{
					_logger.LogDebug("User canceled Save As dialog for: {DisplayName}", doc.DisplayName);
					return;
				}

				_logger.LogInformation("Saving document to new location: {TargetPath} ({RowCount} rows)",
					targetPath, rows.Count);

				await SaveJsonToPathAsync(rows, targetPath, _jsonService, _logger, cancellationToken)
					.ConfigureAwait(false);

				_logger.LogInformation("Document saved successfully to: {TargetPath}", targetPath);
				break;
			}
			default:
				_logger.LogError("Unsupported file type for Save As: {Extension}", doc.Extension);
				throw new NotSupportedException($"Unsupported file type '{doc.Extension}'.");
		}
	}

	/// <summary>
	///     Loads a JSON document and converts it to editable grid rows.
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A tuple containing the loaded rows and original file bytes.</returns>
	private async Task<(IReadOnlyList<GridRowData> rows, byte[] originalBytes)> LoadJsonAsync(
		EditorDocumentReference doc,
		CancellationToken cancellationToken)
	{
		_logger.LogDebug("Loading JSON bytes for: {DisplayName}", doc.DisplayName);

		// Keep an original snapshot for dirty comparison; Core JSON loader is tolerant/validated.
		var bytes = await LoadBytesAsync(doc, cancellationToken).ConfigureAwait(false);
		if (bytes.Length == 0)
		{
			_logger.LogWarning("Document is empty: {DisplayName}", doc.DisplayName);
			return ([], bytes);
		}

		_logger.LogDebug("Loaded {ByteCount} bytes for: {DisplayName}", bytes.Length, doc.DisplayName);

		// For workspace files, use Core JsonService to honor existing JsonModel schema.
		if (doc.IsWorkspaceFile)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(doc.FilePath);

			_logger.LogDebug("Parsing workspace JSON file: {FilePath}", doc.FilePath);

			var model = await _jsonService.LoadAsync(doc.FilePath!, cancellationToken).ConfigureAwait(false);

			var rows = new List<GridRowData>(model.Coordinates.Length);
			foreach (var c in model.Coordinates)
				rows.Add(new GridRowData(c.Name, c.X, c.Y, c.Z));

			_logger.LogDebug("Parsed {RowCount} coordinates from workspace file.", rows.Count);
			return (rows, bytes);
		}

		// Archive JSON editing: supported for load + SaveAs (export). Parse minimal schema from bytes.
		_logger.LogDebug("Parsing archive JSON entry: {EntryName}", doc.ArchiveEntryFullName);

		var fallback = JsonSerializer.Deserialize<CoordinatesModel[]>(bytes,
			new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				NumberHandling = JsonNumberHandling.AllowReadingFromString
			}) ?? [];

		var archiveRows = new List<GridRowData>(fallback.Length);
		foreach (var c in fallback)
			archiveRows.Add(new GridRowData(c.Name, c.X, c.Y, c.Z));

		_logger.LogDebug("Parsed {RowCount} coordinates from archive entry.", archiveRows.Count);
		return (archiveRows, bytes);
	}

	/// <summary>
	///     Loads raw bytes from a document (workspace file or archive entry).
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The raw file bytes.</returns>
	private async Task<byte[]> LoadBytesAsync(EditorDocumentReference doc, CancellationToken cancellationToken)
	{
		if (doc.IsWorkspaceFile)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(doc.FilePath);
			_logger.LogTrace("Reading bytes from workspace file: {FilePath}", doc.FilePath);
			return await File.ReadAllBytesAsync(doc.FilePath!, cancellationToken).ConfigureAwait(false);
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(doc.ArchivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(doc.ArchiveEntryFullName);

		_logger.LogTrace("Reading bytes from archive: {ArchivePath} -> {EntryName}",
			doc.ArchivePath, doc.ArchiveEntryFullName);

		return await ReadArchiveEntryBytesAsync(doc.ArchivePath!, doc.ArchiveEntryFullName!, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	///     Saves grid rows to a JSON file at the specified path.
	/// </summary>
	/// <param name="rows">The rows to save.</param>
	/// <param name="filePath">The target file path.</param>
	/// <param name="jsonService">The JSON serialization service.</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	private static async Task SaveJsonToPathAsync(
		IReadOnlyList<GridRowData> rows,
		string filePath,
		IJsonService jsonService,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		logger.LogDebug("Serializing {RowCount} rows to JSON at: {FilePath}", rows.Count, filePath);

		// Preserve JsonModel schema. Keep Name based on filename to avoid losing metadata.
		var name = Path.GetFileNameWithoutExtension(filePath);
		var model = new JsonModel(
			name,
			null,
			rows.Select(r => new CoordinatesModel(r.Name, r.X, r.Y, r.Z)).ToArray());

		await jsonService.SaveAsync(model, filePath, cancellationToken).ConfigureAwait(false);
		logger.LogDebug("JSON serialization completed: {FilePath}", filePath);
	}

	/// <summary>
	///     Reads raw bytes from an archive entry (ZIP/TACO).
	/// </summary>
	/// <param name="archivePath">The path to the archive file.</param>
	/// <param name="entryFullName">The full name of the entry within the archive.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The raw entry bytes, or an empty array if the entry is not found.</returns>
	private static async Task<byte[]> ReadArchiveEntryBytesAsync(string archivePath, string entryFullName,
		CancellationToken cancellationToken)
	{
		await using var zip = await ZipFile.OpenReadAsync(archivePath, cancellationToken).ConfigureAwait(false);
		var entry = zip.GetEntry(entryFullName);
		if (entry is null)
			return [];

		await using var stream = entry.Open();
		await using var ms = new MemoryStream();
		await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
		return ms.ToArray();
	}
}