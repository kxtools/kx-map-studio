namespace KXMapStudio.Libs.Services.WorkshopExplorer;

/// <summary>
///     Service responsible for filesystem scanning, tree building, and validation of workshop files.
/// </summary>
/// <remarks>
///     <para>
///         This service manages the Data folder, recursively scans directories for JSON files,
///         and builds hierarchical tree structures for UI binding.
///     </para>
///     <para>
///         Only JSON files are considered valid workshop files. Temporary files (starting with '.' or '~')
///         are automatically excluded from scans.
///     </para>
/// </remarks>
public sealed class WorkshopExplorerService : IWorkshopExplorerService
{
	/// <summary>
	///     HashSet of allowed file extensions for workshop files (optimized for O(1) lookups).
	/// </summary>
	/// <remarks>
	///     Using HashSet instead of IReadOnlyCollection for better performance with StringComparer.OrdinalIgnoreCase.
	/// </remarks>
	private static readonly HashSet<string> AllowedWorkshopExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		FileExtension.Json
	};

	private readonly ILogger<WorkshopExplorerService> _logger;

	/// <summary>
	///     Initializes a new instance of the <see cref="WorkshopExplorerService" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger" /> is <see langword="null" />.</exception>
	public WorkshopExplorerService(ILogger<WorkshopExplorerService> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);

		_logger = logger;

		DataFolder = Path.Combine(AppContext.BaseDirectory, Constants.Settings.DataFolder);

		// Ensure Data folder exists
		if (!Directory.Exists(DataFolder))
		{
			Directory.CreateDirectory(DataFolder);
			_logger.LogInformation("Created Data folder at: {DataFolder}", DataFolder);
		}
		else
		{
			_logger.LogDebug("Data folder exists at: {DataFolder}", DataFolder);
		}
	}

	/// <summary>
	///     Gets the absolute path to the Data folder where workshop files are stored.
	/// </summary>
	public string DataFolder { get; }

	/// <summary>
	///     Determines whether a filesystem change is relevant to the workshop explorer.
	/// </summary>
	/// <param name="fullPath">The full path of the changed file or directory.</param>
	/// <returns><see langword="true" /> if the change is relevant; otherwise, <see langword="false" />.</returns>
	/// <remarks>
	///     Temporary files (starting with '.' or '~') are ignored.
	///     Only allowed file types (JSON) trigger refresh events.
	/// </remarks>
	public bool IsRelevantChange(string fullPath)
	{
		if (string.IsNullOrWhiteSpace(fullPath))
		{
			_logger.LogTrace("IsRelevantChange: Path is null or whitespace.");
			return false;
		}

		var fileName = Path.GetFileName(fullPath);

		// Ignore temporary files
		if (fileName.StartsWith('.') || fileName.StartsWith('~'))
		{
			_logger.LogTrace("IsRelevantChange: Ignoring temporary file: {FileName}", fileName);
			return false;
		}

		var isRelevant = IsAllowedFilePath(fullPath);
		_logger.LogTrace("IsRelevantChange: {FullPath} -> {IsRelevant}", fullPath, isRelevant);

		return isRelevant;
	}

	/// <summary>
	///     Determines whether a file path corresponds to an allowed workshop file type.
	/// </summary>
	/// <param name="fullPath">The full path to validate.</param>
	/// <returns><see langword="true" /> if the path is a directory or an allowed file; otherwise, <see langword="false" />.</returns>
	public bool IsAllowedFilePath(string fullPath)
	{
		if (string.IsNullOrWhiteSpace(fullPath))
			return false;

		// Allow directories for tree navigation
		if (Directory.Exists(fullPath))
			return true;

		// Filter files by extension
		var extension = Path.GetExtension(fullPath);
		return AllowedWorkshopExtensions.Contains(extension);
	}

	/// <summary>
	///     Asynchronously scans a directory and builds a hierarchical tree structure of workshop files.
	/// </summary>
	/// <param name="directoryPath">The directory path to scan.</param>
	/// <param name="cancellationToken">A token to cancel the scan operation.</param>
	/// <returns>A <see cref="Task{T}" /> representing the asynchronous operation, containing the root scan node.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath" /> is null or whitespace.</exception>
	/// <exception cref="OperationCanceledException">
	///     Thrown when the operation is canceled via
	///     <paramref name="cancellationToken" />.
	/// </exception>
	public async Task<WorkshopExplorerScanNode> ScanDirectoryAsync(string directoryPath,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

		_logger.LogInformation("Starting directory scan: {DirectoryPath}", directoryPath);

		var result = await Task.Run(() => ScanDirectoryRecursive(directoryPath, cancellationToken), cancellationToken)
			.ConfigureAwait(false);

		_logger.LogInformation("Directory scan completed: {DirectoryPath}. Total children: {Count}",
			directoryPath, result.Children.Count);

		return result;
	}

	/// <summary>
	///     Recursively scans a directory and its subdirectories, building a tree of workshop files.
	/// </summary>
	/// <param name="directoryPath">The directory path to scan.</param>
	/// <param name="cancellationToken">A token to cancel the scan operation.</param>
	/// <returns>A <see cref="WorkshopExplorerScanNode" /> representing the directory tree.</returns>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	private WorkshopExplorerScanNode ScanDirectoryRecursive(string directoryPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var dirInfo = new DirectoryInfo(directoryPath);
		var children = new List<WorkshopExplorerScanNode>();

		// Scan subdirectories first
		try
		{
			foreach (var subDir in dirInfo.EnumerateDirectories()
				         .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var subDirNode = ScanDirectoryRecursive(subDir.FullName, cancellationToken);

				// Only include directories that contain JSON files (directly or in subdirectories)
				if (subDirNode.Children.Count > 0)
					children.Add(subDirNode);
			}
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogWarning(ex, "Access denied to directory: {DirectoryPath}", directoryPath);
		}

		// Scan files (only JSON)
		try
		{
			foreach (var file in dirInfo.EnumerateFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
			{
				cancellationToken.ThrowIfCancellationRequested();

				// HashSet with OrdinalIgnoreCase comparer handles case-insensitive lookup efficiently
				if (AllowedWorkshopExtensions.Contains(file.Extension))
					children.Add(new WorkshopExplorerScanNode(
						file.Name,
						file.FullName,
						false,
						[]));
			}
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogWarning(ex, "Access denied to files in directory: {DirectoryPath}", directoryPath);
		}

		return new WorkshopExplorerScanNode(
			dirInfo.Name,
			dirInfo.FullName,
			true,
			children);
	}
}