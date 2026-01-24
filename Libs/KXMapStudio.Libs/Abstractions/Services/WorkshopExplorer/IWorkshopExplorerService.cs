namespace KXMapStudio.Libs.Abstractions.Services.WorkshopExplorer;

/// <summary>
///     Defines the contract for filesystem scanning and validation of workshop files.
/// </summary>
/// <remarks>
///     <para>
///         This service manages the Data folder, recursively scans directories for valid workshop files,
///         and builds hierarchical tree structures for UI binding.
///     </para>
///     <para>
///         Only JSON files (<c>.json</c>) are considered valid workshop files.
///         Temporary files (starting with '.' or '~') are automatically excluded from scans.
///     </para>
/// </remarks>
public interface IWorkshopExplorerService
{
	/// <summary>
	///     Gets the absolute path to the Data folder where workshop files are stored.
	/// </summary>
	/// <remarks>
	///     The folder is created automatically if it doesn't exist during service initialization.
	/// </remarks>
	string DataFolder { get; }

	/// <summary>
	///     Determines whether a filesystem change is relevant to the workshop explorer.
	/// </summary>
	/// <param name="fullPath">The full path of the changed file or directory.</param>
	/// <returns>
	///     <see langword="true" /> if the change affects workshop files (JSON files);
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <remarks>
	///     <para>
	///         This method is used by file system watchers to filter irrelevant changes.
	///     </para>
	///     <para>
	///         Temporary files (starting with '.' or '~') are automatically ignored.
	///         Only allowed file types (JSON) trigger refresh events.
	///     </para>
	/// </remarks>
	bool IsRelevantChange(string fullPath);

	/// <summary>
	///     Determines whether a file path corresponds to an allowed workshop file type.
	/// </summary>
	/// <param name="fullPath">The full path to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the path is a directory or an allowed file (JSON);
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <remarks>
	///     Directories are always considered valid to enable tree navigation.
	///     Files are validated by extension (case-insensitive).
	/// </remarks>
	bool IsAllowedFilePath(string fullPath);

	/// <summary>
	///     Asynchronously scans a directory and builds a hierarchical tree structure of workshop files.
	/// </summary>
	/// <param name="directoryPath">The directory path to scan.</param>
	/// <param name="cancellationToken">A token to cancel the scan operation.</param>
	/// <returns>
	///     A task containing the root scan node with all child nodes representing the directory structure.
	/// </returns>
	/// <remarks>
	///     <para>
	///         The scan is recursive and includes subdirectories.
	///         Empty directories (containing no JSON files) are excluded from results.
	///     </para>
	///     <para>
	///         The operation can be canceled via <paramref name="cancellationToken" />.
	///     </para>
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath" /> is null or whitespace.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	Task<WorkshopExplorerScanNode> ScanDirectoryAsync(string directoryPath,
		CancellationToken cancellationToken = default);
}