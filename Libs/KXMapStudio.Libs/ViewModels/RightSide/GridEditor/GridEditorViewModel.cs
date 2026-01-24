namespace KXMapStudio.Libs.ViewModels.RightSide.GridEditor;

/// <summary>
///     ViewModel for the grid editor, managing coordinate editing, undo/redo, and Mumble integration.
/// </summary>
/// <remarks>
///     <para>
///         This ViewModel is the central orchestrator for coordinate grid editing, providing:
///         <list type="bullet">
///             <item>Document loading/saving (JSON files and archive entries)</item>
///             <item>Undo/redo state management with intelligent dirty tracking</item>
///             <item>Row manipulation commands (Add, Delete, Move Up/Down)</item>
///             <item>Mumble integration for real-time marker addition via F9 hotkey</item>
///         </list>
///     </para>
///     <para>
///         Implements <see cref="IDisposable" /> to properly unsubscribe from service events
///         and dispose cancellation tokens.
///     </para>
/// </remarks>
public sealed partial class GridEditorViewModel : ObservableObject, IGridEditorViewModel
{
	private readonly ISaveFileDialogService _dialogService;
	private readonly IDispatcherHelper _dispatcherHelper;
	private readonly IGridEditorDocumentService _documentService;
	private readonly IGlobalHotkeyService _hotkeyService;
	private readonly ILogger<GridEditorViewModel> _logger;
	private readonly IMumbleMarkerService _mumbleMarkerService;
	private readonly IMumbleService _mumbleService;
	private readonly INotificationService _notificationService;
	private readonly IGridRowManipulationService _rowManipulationService;
	private readonly IStateManagementService<IReadOnlyList<GridEditorRowViewModel>> _state;

	private int _autoMarkerCounter;
	private CancellationTokenSource? _cts;
	private EditorDocumentReference? _currentDoc;

	/// <summary>
	///     Gets or sets the document title displayed in the editor header.
	/// </summary>
	[ObservableProperty] private string? _documentTitle;

	/// <summary>
	///     Gets or sets the file extension of the opened document (e.g., ".json").
	/// </summary>
	[ObservableProperty] private string _fileExtension = string.Empty;

	/// <summary>
	///     Gets or sets a value indicating whether the document has unsaved changes.
	/// </summary>
	[ObservableProperty] private bool _isDirty;

	/// <summary>
	///     Gets or sets a value indicating whether a document is currently loaded.
	/// </summary>
	[ObservableProperty] private bool _isLoaded;

	private IReadOnlyList<GridEditorRowViewModel>? _lastKnownState;

	/// <summary>
	///     Gets or sets the load time statistics text (e.g., "Loaded in 25 ms").
	/// </summary>
	[ObservableProperty] private string _openedFileLoadTime = string.Empty;

	/// <summary>
	///     Gets or sets the name of the currently opened file.
	/// </summary>
	[ObservableProperty] private string? _openedFileName;

	/// <summary>
	///     Gets or sets the full path of the currently opened file.
	/// </summary>
	[ObservableProperty] private string? _openedFilePath;

	private int _suppressDirty;

	/// <summary>
	///     Initializes a new instance of the <see cref="GridEditorViewModel" /> class.
	/// </summary>
	/// <param name="documentService">The document service for loading and saving files.</param>
	/// <param name="state">The state management service for undo/redo operations.</param>
	/// <param name="mumbleService">The Mumble service for Guild Wars 2 integration.</param>
	/// <param name="mumbleMarkerService">The service for creating markers from Mumble data.</param>
	/// <param name="rowManipulationService">The service for row manipulation operations.</param>
	/// <param name="hotkeyService">The hotkey service for F9 marker addition.</param>
	/// <param name="dispatcherHelper">The dispatcher helper for UI thread synchronization.</param>
	/// <param name="dialogService">The dialog service for user confirmations.</param>
	/// <param name="notificationService">The notification service for displaying user feedback.</param>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when any constructor parameter is <see langword="null" />.
	/// </exception>
	public GridEditorViewModel(
		IGridEditorDocumentService documentService,
		IStateManagementService<IReadOnlyList<GridEditorRowViewModel>> state,
		IMumbleService mumbleService,
		IMumbleMarkerService mumbleMarkerService,
		IGridRowManipulationService rowManipulationService,
		IGlobalHotkeyService hotkeyService,
		IDispatcherHelper dispatcherHelper,
		ISaveFileDialogService dialogService,
		INotificationService notificationService,
		ILogger<GridEditorViewModel> logger)
	{
		ArgumentNullException.ThrowIfNull(documentService);
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(mumbleService);
		ArgumentNullException.ThrowIfNull(mumbleMarkerService);
		ArgumentNullException.ThrowIfNull(rowManipulationService);
		ArgumentNullException.ThrowIfNull(hotkeyService);
		ArgumentNullException.ThrowIfNull(dispatcherHelper);
		ArgumentNullException.ThrowIfNull(dialogService);
		ArgumentNullException.ThrowIfNull(notificationService);
		ArgumentNullException.ThrowIfNull(logger);

		_documentService = documentService;
		_state = state;
		_mumbleService = mumbleService;
		_mumbleMarkerService = mumbleMarkerService;
		_rowManipulationService = rowManipulationService;
		_hotkeyService = hotkeyService;
		_dispatcherHelper = dispatcherHelper;
		_dialogService = dialogService;
		_notificationService = notificationService;
		_logger = logger;

		Rows = [];

		SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
		SaveAsCommand = new AsyncRelayCommand(SaveAsAsync, () => CanSaveAs);
		UndoCommand = new RelayCommand(Undo, () => CanUndo);
		RedoCommand = new RelayCommand(Redo, () => CanRedo);
		MoveUpCommand = new RelayCommand<GridEditorRowViewModel?>(MoveUp, CanMoveUp);
		MoveDownCommand = new RelayCommand<GridEditorRowViewModel?>(MoveDown, CanMoveDown);
		AddRowCommand = new RelayCommand(AddRow, () => IsLoaded);
		DeleteRowCommand = new RelayCommand<GridEditorRowViewModel?>(DeleteRow, CanDeleteRow);
		AddMarkerFromMumbleCommand = new RelayCommand(AddMarkerFromMumble, () => CanAddMarkerFromMumble);
		InsertRowAboveCommand = new RelayCommand<GridEditorRowViewModel?>(InsertRowAbove, CanInsertRow);
		InsertRowBelowCommand = new RelayCommand<GridEditorRowViewModel?>(InsertRowBelow, CanInsertRow);
		CloseFileCommand = new AsyncRelayCommand(CloseFileAsync, () => IsLoaded);

		_state.StateChanged += StateOnStateChanged;
		_mumbleService.MumbleUpdated += MumbleServiceOnMumbleUpdated;
		_hotkeyService.AddMarkerFromMumblePressed += (_, _) => AddMarkerFromMumbleCommand.Execute(null);

		_logger.LogInformation("GridEditorViewModel initialized with state capacity: {Capacity}", _state.Capacity);
	}

	private bool CanAddMarkerFromMumble =>
		IsLoaded && _mumbleMarkerService.CanCreateMarkerFromMumbleState(_mumbleService.Current);

	/// <summary>
	///     Gets the observable collection of grid rows for UI binding.
	/// </summary>
	public ObservableCollection<GridEditorRowViewModel> Rows { get; }

	/// <summary>
	///     Gets a value indicating whether an undo operation is available.
	/// </summary>
	public bool CanUndo => _state.CanUndo;

	/// <summary>
	///     Gets a value indicating whether a redo operation is available.
	/// </summary>
	public bool CanRedo => _state.CanRedo;

	/// <summary>
	///     Gets a value indicating whether the document can be saved.
	/// </summary>
	/// <remarks>
	///     Save is only enabled for workspace JSON files with unsaved changes.
	///     Archive entries require "Save As" to export.
	/// </remarks>
	public bool CanSave => IsLoaded && IsDirty && _currentDoc is { IsWorkspaceFile: true }
	                       && string.Equals(_currentDoc.Extension, Core.Types.Structs.FileExtension.Json,
		                       StringComparison.OrdinalIgnoreCase);

	/// <summary>
	///     Gets a value indicating whether the document can be saved to a new location.
	/// </summary>
	public bool CanSaveAs => IsLoaded;

	/// <summary>
	///     Gets the command to save the document to its original location.
	/// </summary>
	public IAsyncRelayCommand SaveCommand { get; }

	/// <summary>
	///     Gets the command to save the document to a new location (Save As).
	/// </summary>
	public IAsyncRelayCommand SaveAsCommand { get; }

	/// <summary>
	///     Gets the command to undo the last edit operation.
	/// </summary>
	public IRelayCommand UndoCommand { get; }

	/// <summary>
	///     Gets the command to redo a previously undone operation.
	/// </summary>
	public IRelayCommand RedoCommand { get; }

	/// <summary>
	///     Gets the command to move a row up in the grid.
	/// </summary>
	public IRelayCommand<GridEditorRowViewModel?> MoveUpCommand { get; }

	/// <summary>
	///     Gets the command to move a row down in the grid.
	/// </summary>
	public IRelayCommand<GridEditorRowViewModel?> MoveDownCommand { get; }

	/// <summary>
	///     Gets the command to add a new empty row to the grid.
	/// </summary>
	public IRelayCommand AddRowCommand { get; }

	/// <summary>
	///     Gets the command to delete a selected row from the grid.
	/// </summary>
	public IRelayCommand<GridEditorRowViewModel?> DeleteRowCommand { get; }

	/// <summary>
	///     Gets the command to add a marker from the current Mumble position.
	/// </summary>
	public IRelayCommand AddMarkerFromMumbleCommand { get; }

	/// <summary>
	///     Gets the command to insert a new row above the selected row.
	/// </summary>
	public IRelayCommand<GridEditorRowViewModel?> InsertRowAboveCommand { get; }

	/// <summary>
	///     Gets the command to insert a new row below the selected row.
	/// </summary>
	public IRelayCommand<GridEditorRowViewModel?> InsertRowBelowCommand { get; }

	/// <summary>
	///     Gets the command to close the currently opened file.
	/// </summary>
	public IAsyncRelayCommand CloseFileCommand { get; }

	/// <summary>
	///     Occurs when a new row is added to the grid (via Add Row or Add from Mumble).
	/// </summary>
	/// <remarks>
	///     Subscribers can use this event to automatically focus the newly added row in the UI.
	/// </remarks>
	public event EventHandler<GridEditorRowViewModel>? RowAdded;

	/// <summary>
	///     Asynchronously loads a document into the grid editor.
	/// </summary>
	/// <param name="doc">The document reference to load.</param>
	/// <param name="cancellationToken">A token to cancel the load operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="doc" /> is <see langword="null" />.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	public async Task LoadAsync(EditorDocumentReference doc, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(doc);

		_logger.LogInformation("Loading document: {DisplayName} (Extension: {Extension}, IsArchive: {IsArchive})",
			doc.DisplayName, doc.Extension, doc.IsArchiveEntry);

		_cts?.Cancel();
		_cts?.Dispose();
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		// Reset history when switching documents.
		_state.Reset();

		_currentDoc = doc;
		DocumentTitle = doc.IsArchiveEntry
			? $"{doc.DisplayName} (in {Path.GetFileName(doc.ArchivePath)}) [Read-Only]"
			: doc.DisplayName;

		// Set file metadata for UI display
		OpenedFileName = doc.DisplayName;
		OpenedFilePath = doc.FilePath;
		FileExtension = doc.Extension.ToLowerInvariant();

		IsLoaded = true;

		SetDirty(false);
		NotifyCommandStateChanged();

		var ct = _cts.Token;

		// Measure load time
		var sw = Stopwatch.StartNew();
		var (rowData, _) = await _documentService.LoadAsync(doc, ct);
		sw.Stop();
		ct.ThrowIfCancellationRequested();

		OpenedFileLoadTime = $"Loaded in {sw.ElapsedMilliseconds} ms";

		_logger.LogInformation("Document loaded: {DisplayName}. Rows: {RowCount}, Load time: {LoadTimeMs}ms",
			doc.DisplayName, rowData.Count, sw.ElapsedMilliseconds);

		// Convert data models to ViewModels
		var rowViewModels = ConvertDataToViewModels(rowData);
		ReloadRows(rowViewModels);

		// Set the original state baseline for intelligent dirty tracking
		_state.SetOriginalState(SnapshotRows());

		// Initialize last known state for property change tracking
		_lastKnownState = SnapshotRows();

		SetDirty(false);
		NotifyCommandStateChanged();
	}

	/// <summary>
	///     Disposes resources, unsubscribes from events, and cancels pending operations.
	/// </summary>
	public void Dispose()
	{
		_logger.LogDebug("Disposing GridEditorViewModel.");

		_state.StateChanged -= StateOnStateChanged;
		_mumbleService.MumbleUpdated -= MumbleServiceOnMumbleUpdated;

		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;

		foreach (var row in Rows)
			row.PropertyChanged -= RowOnPropertyChanged;

		_logger.LogInformation("GridEditorViewModel disposed successfully.");
	}

	private void StateOnStateChanged(object? sender, EventArgs e)
	{
		NotifyCommandStateChanged();
	}

	private void MumbleServiceOnMumbleUpdated(object? sender, MumbleStateModel e)
	{
		// Update command state when Mumble availability changes
		_dispatcherHelper.InvokeOnUIThread(() => AddMarkerFromMumbleCommand.NotifyCanExecuteChanged());
	}

	/// <summary>
	///     Converts data models to ViewModels with sequential IDs.
	/// </summary>
	/// <param name="rowData">The data models to convert.</param>
	/// <returns>A list of ViewModels ready for UI binding.</returns>
	private static IReadOnlyList<GridEditorRowViewModel> ConvertDataToViewModels(IReadOnlyList<GridRowData> rowData)
	{
		var viewModels = new List<GridEditorRowViewModel>(rowData.Count);
		var id = 1;
		foreach (var data in rowData)
			viewModels.Add(new GridEditorRowViewModel
			{
				Id = id++,
				Name = data.Name,
				X = data.X,
				Y = data.Y,
				Z = data.Z
			});
		return viewModels;
	}

	/// <summary>
	///     Converts ViewModels to data models for service layer operations.
	/// </summary>
	/// <param name="viewModels">The ViewModels to convert.</param>
	/// <returns>A list of data models.</returns>
	private static IReadOnlyList<GridRowData> ConvertViewModelsToData(IReadOnlyList<GridEditorRowViewModel> viewModels)
	{
		return viewModels.Select(vm => new GridRowData(vm.Name, vm.X, vm.Y, vm.Z)).ToList();
	}

	private void ReloadRows(IReadOnlyList<GridEditorRowViewModel> rows)
	{
		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			Rows.Clear();

			var id = 1;
			foreach (var r in rows)
			{
				r.Id = id++;
				HookRow(r);
				Rows.Add(r);
			}
		}
	}

	private void HookRow(GridEditorRowViewModel row)
	{
		row.PropertyChanged -= RowOnPropertyChanged;
		row.PropertyChanged += RowOnPropertyChanged;
	}

	private void RowOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (Interlocked.CompareExchange(ref _suppressDirty, 0, 0) == 1)
			return;

		if (sender is not GridEditorRowViewModel)
			return;

		if (e.PropertyName is nameof(GridEditorRowViewModel.Id))
			return;

		// Push the last known state (before this change) to the undo stack
		// This enables undo/redo for individual cell edits
		// 
		// Design Decision: Per-keystroke undo granularity
		// -----------------------------------------------
		// This implementation creates one undo snapshot per property change, which means
		// per-keystroke granularity for text edits. This is intentional based on requirements:
		// "Toute modification doit déclencher un snapshot" (Every modification must trigger a snapshot)
		// 
		// In a data grid context (vs. a text editor), users typically edit individual cells
		// and move between fields, making per-field-edit granularity more appropriate than
		// per-character. Each property change represents a complete field edit in this context.
		// 
		// The StateManagementService has a capacity limit (default: 20 snapshots) to prevent
		// excessive memory usage. For most use cases, this provides a good balance.
		//
		// Future Enhancement: If performance issues arise with very large datasets or rapid
		// sequential edits, consider implementing:
		// - Time-based debouncing (e.g., batch changes within 500ms)
		// - Edit session tracking (capture state on focus gained, push on focus lost)
		if (_lastKnownState is not null)
		{
			_state.PushSnapshot(_lastKnownState);
			_logger.LogTrace("Pushed last known state before property change: {PropertyName}", e.PropertyName);
		}

		// Capture current state as the new "last known state"
		_lastKnownState = SnapshotRows();

		// Smart dirty tracking: check if current state matches original after property change
		var isAtOriginal = _state.IsAtOriginalState(_lastKnownState);
		SetDirty(!isAtOriginal);
	}

	private void PushUndoSnapshot()
	{
		// Snapshot BEFORE a change.
		_state.PushSnapshot(SnapshotRows());
	}

	private IReadOnlyList<GridEditorRowViewModel> SnapshotRows()
	{
		return Rows.Select(CloneRow).ToList();
	}

	private static GridEditorRowViewModel CloneRow(GridEditorRowViewModel r)
	{
		return new GridEditorRowViewModel
		{
			Id = r.Id,
			Name = r.Name,
			X = r.X,
			Y = r.Y,
			Z = r.Z
		};
	}

	private void Undo()
	{
		if (!CanUndo)
			return;

		_logger.LogDebug("Performing undo. Current row count: {RowCount}", Rows.Count);

		var current = SnapshotRows();
		var state = _state.Undo(current);
		ReloadRows(state);

		// Update last known state after undo
		_lastKnownState = SnapshotRows();

		// Smart dirty tracking: check if we're back to the original state
		var isAtOriginal = _state.IsAtOriginalState(_lastKnownState);
		SetDirty(!isAtOriginal);

		_logger.LogDebug("Undo completed. New row count: {RowCount}, IsAtOriginal: {IsAtOriginal}",
			Rows.Count, isAtOriginal);
	}

	private void Redo()
	{
		if (!CanRedo)
			return;

		_logger.LogDebug("Performing redo. Current row count: {RowCount}", Rows.Count);

		var current = SnapshotRows();
		var state = _state.Redo(current);
		ReloadRows(state);

		// Update last known state after redo
		_lastKnownState = SnapshotRows();

		// Smart dirty tracking: check if we're back to the original state
		var isAtOriginal = _state.IsAtOriginalState(_lastKnownState);
		SetDirty(!isAtOriginal);

		_logger.LogDebug("Redo completed. New row count: {RowCount}, IsAtOriginal: {IsAtOriginal}",
			Rows.Count, isAtOriginal);
	}

	private async Task SaveAsync()
	{
		if (_currentDoc is null)
			return;

		if (!CanSave)
			return;

		_logger.LogInformation("Saving document: {DisplayName} ({RowCount} rows)", _currentDoc.DisplayName, Rows.Count);

		// Convert ViewModels to data models for service layer
		var rowData = ConvertViewModelsToData(Rows.ToList());

		// Important: do NOT push an undo snapshot for Save. State history is about edits.
		await _documentService.SaveAsync(_currentDoc, rowData, _cts?.Token ?? CancellationToken.None);

		_logger.LogInformation("Document saved successfully: {DisplayName}", _currentDoc.DisplayName);

		// Update the original state baseline to the current state (file is now clean)
		_state.SetOriginalState(SnapshotRows());

		// Update last known state after save
		_lastKnownState = SnapshotRows();

		SetDirty(false);
		NotifyCommandStateChanged();

		// Show success notification
		_notificationService.ShowSuccess("File successfully saved.");
	}

	private async Task SaveAsAsync()
	{
		if (_currentDoc is null)
			return;

		_logger.LogInformation("Initiating Save As for document: {DisplayName}", _currentDoc.DisplayName);

		// Convert ViewModels to data models for service layer
		var rowData = ConvertViewModelsToData(Rows.ToList());

		await _documentService.SaveAsAsync(_currentDoc, rowData, _cts?.Token ?? CancellationToken.None);
		NotifyCommandStateChanged();

		_logger.LogDebug("Save As completed for document: {DisplayName}", _currentDoc.DisplayName);
	}

	private async Task CloseFileAsync()
	{
		if (!IsLoaded)
			return;

		_logger.LogInformation("Initiating close file operation for: {FileName}", OpenedFileName);

		// Check if there are unsaved changes
		if (IsDirty && !string.IsNullOrEmpty(OpenedFileName))
		{
			_logger.LogDebug("Document has unsaved changes. Prompting user for action.");

			var result = await _dialogService.ShowUnsavedChangesDialogAsync(OpenedFileName);

			switch (result)
			{
				case UnsavedChangesDialogResult.SaveAndContinue:
					_logger.LogInformation("User chose to save and continue.");
					// Execute the save command and wait for it to complete
					if (SaveCommand.CanExecute(null))
						await SaveCommand.ExecuteAsync(null);
					else
						_logger.LogWarning("Save command cannot be executed. File may be read-only.");
					break;

				case UnsavedChangesDialogResult.ContinueWithoutSaving:
					_logger.LogInformation("User chose to continue without saving.");
					// Proceed with closing the file
					break;

				case UnsavedChangesDialogResult.Cancel:
					_logger.LogInformation("User canceled file close.");
					// Cancel the operation - do not close the file
					return;
			}
		}

		// Close the file by resetting the editor state
		_logger.LogInformation("Closing file: {FileName}", OpenedFileName);

		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;

		_state.Reset();
		_currentDoc = null;
		DocumentTitle = null;
		OpenedFileName = null;
		OpenedFilePath = null;
		OpenedFileLoadTime = string.Empty;
		FileExtension = string.Empty;
		IsLoaded = false;
		SetDirty(false);

		Rows.Clear();
		_lastKnownState = null;

		NotifyCommandStateChanged();

		_logger.LogInformation("File closed successfully.");
	}

	private void SetDirty(bool value)
	{
		IsDirty = value;
		NotifyCommandStateChanged();
	}

	private void NotifyCommandStateChanged()
	{
		SaveCommand.NotifyCanExecuteChanged();
		SaveAsCommand.NotifyCanExecuteChanged();
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		MoveUpCommand.NotifyCanExecuteChanged();
		MoveDownCommand.NotifyCanExecuteChanged();
		AddRowCommand.NotifyCanExecuteChanged();
		DeleteRowCommand.NotifyCanExecuteChanged();
		AddMarkerFromMumbleCommand.NotifyCanExecuteChanged();
		CloseFileCommand.NotifyCanExecuteChanged();

		OnPropertyChanged(nameof(CanSave));
		OnPropertyChanged(nameof(CanSaveAs));
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	private bool CanMoveUp(GridEditorRowViewModel? row)
	{
		return _rowManipulationService.CanMoveUp(Rows, row);
	}

	private bool CanMoveDown(GridEditorRowViewModel? row)
	{
		return _rowManipulationService.CanMoveDown(Rows, row);
	}

	private void MoveUp(GridEditorRowViewModel? row)
	{
		if (row is null)
			return;

		var index = _rowManipulationService.GetRowIndex(Rows, row);
		if (index <= 0)
			return;

		PushUndoSnapshot();
		Rows.Move(index, index - 1);
		ReindexIds();
		_lastKnownState = SnapshotRows();
		SetDirty(true);
		MoveUpCommand.NotifyCanExecuteChanged();
		MoveDownCommand.NotifyCanExecuteChanged();
	}

	private void MoveDown(GridEditorRowViewModel? row)
	{
		if (row is null)
			return;

		var index = _rowManipulationService.GetRowIndex(Rows, row);
		if (index < 0 || index >= Rows.Count - 1)
			return;

		PushUndoSnapshot();
		Rows.Move(index, index + 1);
		ReindexIds();
		_lastKnownState = SnapshotRows();
		SetDirty(true);
		MoveUpCommand.NotifyCanExecuteChanged();
		MoveDownCommand.NotifyCanExecuteChanged();
	}

	private void AddRow()
	{
		if (!IsLoaded)
			return;

		_logger.LogDebug("Adding new row. Current row count: {RowCount}", Rows.Count);

		PushUndoSnapshot();

		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			var row = new GridEditorRowViewModel
			{
				Id = Rows.Count + 1,
				Name = string.Empty,
				X = 0d,
				Y = 0d,
				Z = 0d
			};
			HookRow(row);
			Rows.Add(row);

			// Notify that a row was added for UI focus
			RowAdded?.Invoke(this, row);

			_logger.LogInformation("Row added successfully. New row count: {RowCount}", Rows.Count);
		}

		// Update last known state after structural change
		_lastKnownState = SnapshotRows();

		SetDirty(true);
	}

	private bool CanDeleteRow(GridEditorRowViewModel? row)
	{
		return IsLoaded && row != null && Rows.Contains(row);
	}

	private void DeleteRow(GridEditorRowViewModel? row)
	{
		if (row is null || !Rows.Contains(row))
			return;

		_logger.LogDebug("Deleting row: Id={Id}, Name={Name}. Current count: {RowCount}",
			row.Id, row.Name, Rows.Count);

		PushUndoSnapshot();

		row.PropertyChanged -= RowOnPropertyChanged;
		Rows.Remove(row);

		ReindexIds();
		_lastKnownState = SnapshotRows();
		SetDirty(true);
		NotifyCommandStateChanged();

		_logger.LogInformation("Row deleted successfully. New row count: {RowCount}", Rows.Count);
	}

	private void ReindexIds()
	{
		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			_rowManipulationService.ReindexIds(Rows);
		}
	}

	private void AddMarkerFromMumble()
	{
		if (!CanAddMarkerFromMumble)
			return;

		var mumbleState = _mumbleService.Current;

		_logger.LogInformation(
			"Adding marker from Mumble: Map={MapId}, Position=({X:0.##}, {Y:0.##}, {Z:0.##})",
			mumbleState.CurrentMapId,
			mumbleState.PlayerPosition.X,
			mumbleState.PlayerPosition.Y,
			mumbleState.PlayerPosition.Z);

		PushUndoSnapshot();

		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			if (Rows.Count == 0)
				_autoMarkerCounter = 0;
			_autoMarkerCounter++;

			// Use MumbleMarkerService to extract coordinates (business logic in Core)
			var (x, y, z) = _mumbleMarkerService.ExtractCoordinatesFromMumble(mumbleState);
			var markerName = _mumbleMarkerService.GenerateMarkerName(_autoMarkerCounter);

			var row = new GridEditorRowViewModel
			{
				Id = Rows.Count + 1,
				Name = markerName,
				X = x,
				Y = y,
				Z = z
			};
			HookRow(row);
			Rows.Add(row);

			// Notify that a row was added for UI focus
			RowAdded?.Invoke(this, row);

			_logger.LogInformation("Marker added from Mumble: Name={Name}, New row count: {RowCount}",
				row.Name, Rows.Count);
		}

		_lastKnownState = SnapshotRows();
		SetDirty(true);

		// Show success notification
		_notificationService.ShowSuccess("Marker successfully added to the list.");
	}

	private bool CanInsertRow(GridEditorRowViewModel? row)
	{
		return IsLoaded && row != null && Rows.Contains(row);
	}

	private void InsertRowAbove(GridEditorRowViewModel? targetRow)
	{
		if (targetRow is null || !Rows.Contains(targetRow))
			return;

		var index = Rows.IndexOf(targetRow);
		if (index < 0)
			return;

		_logger.LogDebug("Inserting new row above index {Index}. Current count: {RowCount}", index, Rows.Count);

		PushUndoSnapshot();

		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			var newRow = new GridEditorRowViewModel
			{
				Id = index + 1, // Temporary, will be reindexed
				Name = string.Empty,
				X = 0d,
				Y = 0d,
				Z = 0d
			};
			HookRow(newRow);
			Rows.Insert(index, newRow);

			_logger.LogInformation("Row inserted above index {Index}. New row count: {RowCount}", index, Rows.Count);
		}

		ReindexIds();
		_lastKnownState = SnapshotRows();
		SetDirty(true);
		NotifyCommandStateChanged();
	}

	private void InsertRowBelow(GridEditorRowViewModel? targetRow)
	{
		if (targetRow is null || !Rows.Contains(targetRow))
			return;

		var index = Rows.IndexOf(targetRow);
		if (index < 0)
			return;

		_logger.LogDebug("Inserting new row below index {Index}. Current count: {RowCount}", index, Rows.Count);

		PushUndoSnapshot();

		using (new DirtyStateSuppression(() => Interlocked.Exchange(ref _suppressDirty, 1),
			       () => Interlocked.Exchange(ref _suppressDirty, 0)))
		{
			var newRow = new GridEditorRowViewModel
			{
				Id = index + 2, // Temporary, will be reindexed
				Name = string.Empty,
				X = 0d,
				Y = 0d,
				Z = 0d
			};
			HookRow(newRow);
			Rows.Insert(index + 1, newRow);

			_logger.LogInformation("Row inserted below index {Index}. New row count: {RowCount}", index, Rows.Count);
		}

		ReindexIds();
		_lastKnownState = SnapshotRows();
		SetDirty(true);
		NotifyCommandStateChanged();
	}
}