namespace KXMapStudio.Libs.Abstractions.ViewModels.RightSide.GridEditor;

public interface IGridEditorViewModel : IDisposable
{
	string? DocumentTitle { get; }
	bool IsLoaded { get; }
	bool IsDirty { get; }

	string? OpenedFileName { get; }
	string? OpenedFilePath { get; }
	string OpenedFileLoadTime { get; }
	string FileExtension { get; }

	bool CanUndo { get; }
	bool CanRedo { get; }

	bool CanSave { get; }
	bool CanSaveAs { get; }

	IAsyncRelayCommand SaveCommand { get; }
	IAsyncRelayCommand SaveAsCommand { get; }
	IAsyncRelayCommand CloseFileCommand { get; }
	IRelayCommand UndoCommand { get; }
	IRelayCommand RedoCommand { get; }
	IRelayCommand<GridEditorRowViewModel?> MoveUpCommand { get; }
	IRelayCommand<GridEditorRowViewModel?> MoveDownCommand { get; }
	IRelayCommand AddRowCommand { get; }
	IRelayCommand<GridEditorRowViewModel?> DeleteRowCommand { get; }
	IRelayCommand AddMarkerFromMumbleCommand { get; }
	IRelayCommand<GridEditorRowViewModel?> InsertRowAboveCommand { get; }
	IRelayCommand<GridEditorRowViewModel?> InsertRowBelowCommand { get; }

	ObservableCollection<GridEditorRowViewModel> Rows { get; }

	Task LoadAsync(EditorDocumentReference doc, CancellationToken cancellationToken = default);

	/// <summary>
	///     Event raised when a new row is added to the grid (via Add Row or Add from Mumble).
	/// </summary>
	event EventHandler<GridEditorRowViewModel>? RowAdded;
}