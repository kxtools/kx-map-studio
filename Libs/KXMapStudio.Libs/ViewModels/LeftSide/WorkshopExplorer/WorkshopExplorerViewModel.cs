namespace KXMapStudio.Libs.ViewModels.LeftSide.WorkshopExplorer;

/// <summary>
///     ViewModel for the Workshop Explorer, managing a unified file viewer for the Data folder.
/// </summary>
/// <remarks>
///     <para>
///         This ViewModel monitors the filesystem for changes, maintains an expandable tree of JSON files,
///         and provides commands for file creation, deletion, and refresh operations.
///     </para>
///     <para>
///         Implements <see cref="IDisposable" /> to properly dispose <see cref="FileSystemWatcher" />,
///         timers, and cancellation tokens.
///     </para>
/// </remarks>
public sealed partial class WorkshopExplorerViewModel : ObservableObject, IWorkshopExplorerViewModel
{
	// Services
	private readonly ISaveFileDialogService _dialogService;
	private readonly IDispatcherHelper _dispatcherHelper;

	// State management
	private readonly HashSet<string> _expandedFolderPaths = new(StringComparer.OrdinalIgnoreCase);
	private readonly IFileStorageRepository _fileStorageRepository;
	private readonly IFileValidationService _fileValidationService;
	private readonly IJsonService _jsonService;
	private readonly ILogger<WorkshopExplorerViewModel> _logger;
	private readonly INotificationService _notificationService;
	private readonly IOpenDocumentTracker _openDocumentTracker;
	private readonly Timer _refreshTimer;

	// File system monitoring
	private readonly FileSystemWatcher _watcher;
	private readonly IWorkshopExplorerService _workshopExplorerService;

	// Observable properties
	/// <summary>
	///     Gets or sets a value indicating whether a refresh operation is in progress.
	/// </summary>
	[ObservableProperty] private bool _isRefreshing;

	/// <summary>
	///     Gets or sets the last error message encountered during operations.
	/// </summary>
	[ObservableProperty] private string? _lastErrorMessage;

	/// <summary>
	///     Gets or sets the UTC timestamp of the last successful refresh.
	/// </summary>
	[ObservableProperty] private DateTimeOffset? _lastRefreshUtc;

	private CancellationTokenSource? _refreshCts;

	/// <summary>
	///     Initializes a new instance of the <see cref="WorkshopExplorerViewModel" /> class.
	/// </summary>
	/// <param name="workshopExplorerService">The service for scanning and validating workshop files.</param>
	/// <param name="fileStorageRepository">The repository for file deletion operations.</param>
	/// <param name="dialogService">The dialog service for file creation and deletion confirmation.</param>
	/// <param name="jsonService">The facade for creating new JSON files.</param>
	/// <param name="dispatcherHelper">The dispatcher helper for UI thread synchronization.</param>
	/// <param name="openDocumentTracker">The service for tracking which file is currently open.</param>
	/// <param name="fileValidationService">The service for file validation operations.</param>
	/// <param name="notificationService">The service for displaying user notifications.</param>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when any constructor parameter is <see langword="null" />.
	/// </exception>
	public WorkshopExplorerViewModel(
		IWorkshopExplorerService workshopExplorerService,
		IFileStorageRepository fileStorageRepository,
		ISaveFileDialogService dialogService,
		IJsonService jsonService,
		IDispatcherHelper dispatcherHelper,
		IOpenDocumentTracker openDocumentTracker,
		IFileValidationService fileValidationService,
		INotificationService notificationService,
		ILogger<WorkshopExplorerViewModel> logger)
	{
		ArgumentNullException.ThrowIfNull(workshopExplorerService);
		ArgumentNullException.ThrowIfNull(fileStorageRepository);
		ArgumentNullException.ThrowIfNull(dialogService);
		ArgumentNullException.ThrowIfNull(jsonService);
		ArgumentNullException.ThrowIfNull(dispatcherHelper);
		ArgumentNullException.ThrowIfNull(openDocumentTracker);
		ArgumentNullException.ThrowIfNull(fileValidationService);
		ArgumentNullException.ThrowIfNull(notificationService);
		ArgumentNullException.ThrowIfNull(logger);

		_workshopExplorerService = workshopExplorerService;
		_fileStorageRepository = fileStorageRepository;
		_dialogService = dialogService;
		_jsonService = jsonService;
		_dispatcherHelper = dispatcherHelper;
		_openDocumentTracker = openDocumentTracker;
		_fileValidationService = fileValidationService;
		_notificationService = notificationService;
		_logger = logger;

		RootNodes = [];
		RefreshCommand = new AsyncRelayCommand(RefreshAsync);
		SelectNodeCommand = new RelayCommand<RoutedPropertyChangedEventArgs<object>>(OnSelectNode);
		CreateFileCommand = new RelayCommand<WorkshopExplorerNodeModel?>(OnCreateFile, CanCreateFile);
		DeleteFileCommand = new AsyncRelayCommand<WorkshopExplorerNodeModel?>(OnDeleteFileAsync, CanDeleteFile);
		_logger.LogDebug("Initializing FileSystemWatcher for: {DataFolder}", _workshopExplorerService.DataFolder);

		_watcher = new FileSystemWatcher(_workshopExplorerService.DataFolder)
		{
			IncludeSubdirectories = true,
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
		};

		_watcher.Created += OnFileSystemChanged;
		_watcher.Deleted += OnFileSystemChanged;
		_watcher.Changed += OnFileSystemChanged;
		_watcher.Renamed += OnFileSystemRenamed;

		_refreshTimer = new Timer { AutoReset = false };
		_refreshTimer.Elapsed += OnRefreshTimerElapsed;

		_logger.LogInformation("WorkshopExplorerViewModel initialized. Data folder: {DataFolder}",
			_workshopExplorerService.DataFolder);

		// Initial load on UI thread using dispatcher helper
		_ = _dispatcherHelper.InvokeOnUIThreadAsync(async () => await RefreshAsync());
	}

	/// <summary>
	///     Gets the collection of root nodes in the workshop explorer tree.
	/// </summary>
	public ObservableCollection<WorkshopExplorerNodeModel> RootNodes { get; }

	/// <summary>
	///     Gets the command to refresh the workshop explorer tree.
	/// </summary>
	public IRelayCommand RefreshCommand { get; }

	/// <summary>
	///     Gets the command invoked when a tree node is selected.
	/// </summary>
	public IRelayCommand<RoutedPropertyChangedEventArgs<object>> SelectNodeCommand { get; }

	/// <summary>
	///     Gets the command to create a new JSON file.
	/// </summary>
	public IRelayCommand<WorkshopExplorerNodeModel?> CreateFileCommand { get; }

	/// <summary>
	///     Gets the command to delete a selected file.
	/// </summary>
	public IAsyncRelayCommand<WorkshopExplorerNodeModel?> DeleteFileCommand { get; }

	/// <summary>
	///     Disposes resources, unsubscribes from events, and cancels pending refresh operations.
	/// </summary>
	public void Dispose()
	{
		_logger.LogDebug("Disposing WorkshopExplorerViewModel.");

		_watcher.Created -= OnFileSystemChanged;
		_watcher.Deleted -= OnFileSystemChanged;
		_watcher.Changed -= OnFileSystemChanged;
		_watcher.Renamed -= OnFileSystemRenamed;
		_watcher.Dispose();

		_refreshTimer.Elapsed -= OnRefreshTimerElapsed;
		_refreshTimer.Dispose();

		_refreshCts?.Dispose();

		_logger.LogInformation("WorkshopExplorerViewModel disposed successfully.");
	}

	/// <summary>
	///     Occurs when a file is selected in the explorer.
	/// </summary>
	/// <remarks>
	///     This event is raised when a user selects a JSON file in the tree.
	///     Subscribers (e.g., <see cref="LeftSidePanelViewModel" />) can handle this event to load the file.
	/// </remarks>
	public event EventHandler<EditorDocumentReference>? FileSelected;

	/// <summary>
	///     Handles filesystem change events (Created/Deleted/Changed).
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments containing the file path.</param>
	private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
	{
		if (_workshopExplorerService.IsRelevantChange(e.FullPath))
		{
			_logger.LogDebug("Filesystem change detected: {ChangeType} - {FullPath}", e.ChangeType, e.FullPath);
			QueueRefresh(250);
		}
	}

	/// <summary>
	///     Handles filesystem rename events.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments containing old and new paths.</param>
	private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
	{
		if (_workshopExplorerService.IsRelevantChange(e.FullPath) ||
		    _workshopExplorerService.IsRelevantChange(e.OldFullPath))
		{
			_logger.LogDebug("Filesystem rename detected: {OldPath} → {NewPath}", e.OldFullPath, e.FullPath);
			QueueRefresh(250);
		}
	}

	/// <summary>
	///     Queues a refresh operation with debouncing.
	/// </summary>
	/// <param name="debounceMs">The debounce delay in milliseconds.</param>
	private void QueueRefresh(int debounceMs)
	{
		_refreshTimer.Stop();
		_refreshTimer.Interval = debounceMs;
		_refreshTimer.Start();
		_logger.LogTrace("Refresh queued with {DebounceMs}ms debounce.", debounceMs);
	}

	/// <summary>
	///     Handles the refresh timer elapsed event, triggering an async refresh on the UI thread.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments.</param>
	private async void OnRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		await _dispatcherHelper.InvokeOnUIThreadAsync(async () =>
		{
			try
			{
				await RefreshAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unhandled exception during auto-refresh.");
			}
		});
	}

	/// <summary>
	///     Asynchronously refreshes the workshop explorer tree by scanning the Data folder.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task RefreshAsync()
	{
		_logger.LogDebug("Starting refresh operation.");

		// Cancel previous refresh if still running
		_refreshCts?.Cancel();
		_refreshCts?.Dispose();
		_refreshCts = new CancellationTokenSource();

		var token = _refreshCts.Token;

		// Save current expanded state before clearing
		SaveExpandedState(RootNodes);

		IsRefreshing = true;
		LastErrorMessage = null;

		try
		{
			var scanResult =
				await _workshopExplorerService.ScanDirectoryAsync(_workshopExplorerService.DataFolder, token);

			_logger.LogDebug("Scan completed. Building UI tree with {ChildCount} top-level nodes.",
				scanResult.Children.Count);

			// Build UI tree
			RootNodes.Clear();
			foreach (var childScan in scanResult.Children)
			{
				var childNode = BuildNodeRecursive(childScan);
				RootNodes.Add(childNode);
			}

			// Restore expanded state
			RestoreExpandedState(RootNodes);

			LastRefreshUtc = DateTimeOffset.UtcNow;

			_logger.LogInformation("Refresh completed successfully. Total root nodes: {RootCount}", RootNodes.Count);
		}
		catch (OperationCanceledException)
		{
			_logger.LogDebug("Refresh operation was canceled (superseded by newer request).");
		}
		catch (Exception ex)
		{
			LastErrorMessage = ex.Message;
			_logger.LogError(ex, "Refresh operation failed. Error: {ErrorMessage}", ex.Message);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	/// <summary>
	///     Recursively builds a UI node model from a scan result node.
	/// </summary>
	/// <param name="scanNode">The scan result node.</param>
	/// <returns>A UI-bindable node model.</returns>
	private static WorkshopExplorerNodeModel BuildNodeRecursive(WorkshopExplorerScanNode scanNode)
	{
		var node = new WorkshopExplorerNodeModel(scanNode.Name, scanNode.FullPath, scanNode.IsDirectory);

		foreach (var childScan in scanNode.Children)
		{
			var childNode = BuildNodeRecursive(childScan);
			node.Children.Add(childNode);
		}

		return node;
	}

	/// <summary>
	///     Recursively saves the expanded state of all directory nodes.
	/// </summary>
	/// <param name="nodes">The nodes to process.</param>
	private void SaveExpandedState(IEnumerable<WorkshopExplorerNodeModel> nodes)
	{
		foreach (var node in nodes)
		{
			if (node is { IsExpanded: true, IsDirectory: true }) _expandedFolderPaths.Add(node.FullPath);

			SaveExpandedState(node.Children);
		}
	}

	/// <summary>
	///     Recursively restores the expanded state of all directory nodes.
	/// </summary>
	/// <param name="nodes">The nodes to process.</param>
	private void RestoreExpandedState(IEnumerable<WorkshopExplorerNodeModel> nodes)
	{
		foreach (var node in nodes)
		{
			if (node.IsDirectory && _expandedFolderPaths.Contains(node.FullPath)) node.IsExpanded = true;

			RestoreExpandedState(node.Children);
		}
	}

	/// <summary>
	///     Handles tree node selection events, raising <see cref="FileSelected" /> for JSON files.
	/// </summary>
	/// <param name="e">The event arguments containing the selected node.</param>
	private void OnSelectNode(RoutedPropertyChangedEventArgs<object>? e)
	{
		if (e?.NewValue is not WorkshopExplorerNodeModel node)
			return;

		// Only handle file selection (not directories)
		if (node.IsDirectory)
			return;

		// Only handle JSON files
		var ext = node.Extension.ToLowerInvariant();
		if (ext != FileExtension.Json)
			return;

		_logger.LogInformation("File selected in explorer: {FileName}", node.Name);

		// Fire event to notify subscribers (LeftSidePanelViewModel will handle loading into GridEditor)
		var doc = EditorDocumentReference.FromWorkspaceFile(node.FullPath);
		FileSelected?.Invoke(this, doc);
	}

	/// <summary>
	///     Determines whether a new file can be created at the specified node.
	/// </summary>
	/// <param name="node">The node to check (null = root level).</param>
	/// <returns><see langword="true" /> if creation is allowed; otherwise, <see langword="false" />.</returns>
	private static bool CanCreateFile(WorkshopExplorerNodeModel? node)
	{
		// Allow creation at root level (node == null) or in directories
		return node == null || node.IsDirectory;
	}

	/// <summary>
	///     Handles the create file command, showing a dialog and creating a new JSON file.
	/// </summary>
	/// <param name="node">The target node (null = root level).</param>
	private async void OnCreateFile(WorkshopExplorerNodeModel? node)
	{
		// Determine target folder
		var targetFolder = GetTargetFolder(node);

		_logger.LogDebug("Initiating file creation in folder: {TargetFolder}", targetFolder);

		// Show dialog to create JSON file
		var newFilePath = await _dialogService.ShowCreateJsonFileDialogAsync(targetFolder);

		if (string.IsNullOrWhiteSpace(newFilePath))
		{
			_logger.LogDebug("User canceled file creation dialog.");
			return;
		}

		try
		{
			_logger.LogInformation("Creating new JSON file: {FilePath}", newFilePath);
			await CreateJsonFileAsync(newFilePath);
			_logger.LogInformation("JSON file created successfully: {FilePath}", newFilePath);
			// Refresh will be triggered automatically by FileSystemWatcher
		}
		catch (Exception ex)
		{
			LastErrorMessage = $"Failed to create file: {ex.Message}";
			_logger.LogError(ex, "Failed to create JSON file: {FilePath}", newFilePath);
		}
	}

	/// <summary>
	///     Determines the target folder for file creation based on the selected node.
	/// </summary>
	/// <param name="node">The selected node (null = root level).</param>
	/// <returns>The absolute path of the target folder.</returns>
	private string GetTargetFolder(WorkshopExplorerNodeModel? node)
	{
		if (node == null)
			return _workshopExplorerService.DataFolder;

		return node.IsDirectory
			? node.FullPath
			: Path.GetDirectoryName(node.FullPath) ?? _workshopExplorerService.DataFolder;
	}

	/// <summary>
	///     Asynchronously creates a new JSON file with empty coordinates.
	/// </summary>
	/// <param name="filePath">The file path to create.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task CreateJsonFileAsync(string filePath)
	{
		// Create a new JSON file with an empty coordinates array
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		await _jsonService.CreateNewAsync(
			fileName,
			null,
			[],
			filePath);
	}

	/// <summary>
	///     Determines whether a file can be deleted.
	/// </summary>
	/// <param name="node">The node to check.</param>
	/// <returns><see langword="true" /> if the node is a file; otherwise, <see langword="false" />.</returns>
	private static bool CanDeleteFile(WorkshopExplorerNodeModel? node)
	{
		// Only allow deleting files (not directories)
		// File existence is validated in OnDeleteFile to avoid synchronous I/O in CanExecute
		return node is { IsDirectory: false };
	}

	/// <summary>
	///     Handles the delete file command, showing confirmation and deleting the file.
	/// </summary>
	/// <param name="node">The node representing the file to delete.</param>
	private async Task OnDeleteFileAsync(WorkshopExplorerNodeModel? node)
	{
		_logger.LogDebug("OnDeleteFileAsync called with node: {NodeName}", node?.Name ?? "null");

		if (node == null)
		{
			_logger.LogWarning("Delete command invoked with null node.");
			return;
		}

		if (!ValidateFileExists(node.FullPath))
		{
			_logger.LogWarning("File no longer exists: {FullPath}", node.FullPath);
			_notificationService.ShowWarning($"File '{node.Name}' no longer exists.");
			await RefreshAsync();
			return;
		}

		var fileName = Path.GetFileName(node.FullPath);

		_logger.LogDebug("Initiating file deletion: {FileName} (Path: {FullPath})", fileName, node.FullPath);

		// Check if the file is currently open in the editor
		if (IsFileCurrentlyOpen(node.FullPath))
		{
			_logger.LogWarning("Cannot delete file that is currently open: {FileName}", fileName);
			LastErrorMessage = $"Cannot delete '{fileName}' because it is currently open. Please close it first.";
			_notificationService.ShowWarning($"Cannot delete '{fileName}' - file is currently open.");
			return;
		}

		if (!await ConfirmFileDeletion(fileName))
		{
			_logger.LogDebug("User canceled file deletion: {FileName}", fileName);
			return;
		}

		try
		{
			_logger.LogInformation("Deleting file: {FullPath}", node.FullPath);
			await _fileStorageRepository.DeleteFileAsync(node.FullPath);
			_logger.LogInformation("File deleted successfully: {FullPath}", node.FullPath);
			_notificationService.ShowSuccess($"File '{fileName}' deleted successfully.");
			// Refresh will be triggered automatically by FileSystemWatcher
		}
		catch (Exception ex)
		{
			LastErrorMessage = $"Failed to delete file: {ex.Message}";
			_logger.LogError(ex, "Failed to delete file: {FullPath}", node.FullPath);
			_notificationService.ShowWarning($"Failed to delete '{fileName}': {ex.Message}");
		}
	}

	/// <summary>
	///     Validates that a file exists on disk.
	/// </summary>
	/// <param name="filePath">The file path to validate.</param>
	/// <returns><see langword="true" /> if the file exists; otherwise, <see langword="false" />.</returns>
	private bool ValidateFileExists(string filePath)
	{
		return _fileValidationService.FileExists(filePath);
	}

	/// <summary>
	///     Checks if the specified file is currently open in the editor.
	/// </summary>
	/// <param name="filePath">The file path to check.</param>
	/// <returns><see langword="true" /> if the file is currently open; otherwise, <see langword="false" />.</returns>
	private bool IsFileCurrentlyOpen(string filePath)
	{
		return _openDocumentTracker.IsFileOpen(filePath);
	}

	/// <summary>
	///     Shows a confirmation dialog for file deletion.
	/// </summary>
	/// <param name="fileName">The name of the file to delete.</param>
	/// <returns>A task containing <see langword="true" /> if the user confirmed deletion; otherwise, <see langword="false" />.</returns>
	private async Task<bool> ConfirmFileDeletion(string fileName)
	{
		return await _dialogService.ShowDeleteFileConfirmationAsync(fileName);
	}
}