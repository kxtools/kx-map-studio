namespace KXMapStudio.Libs.Abstractions.ViewModels.LeftSide.WorkshopExplorer;

/// <summary>
///     Defines the contract for the Workshop Explorer ViewModel.
/// </summary>
/// <remarks>
///     This interface exposes file tree navigation, file operations, and file selection events
///     for the workshop explorer UI component.
/// </remarks>
public interface IWorkshopExplorerViewModel : IDisposable
{
	/// <summary>
	///     Gets the collection of root nodes in the workshop explorer tree.
	/// </summary>
	ObservableCollection<WorkshopExplorerNodeModel> RootNodes { get; }

	/// <summary>
	///     Gets a value indicating whether a refresh operation is in progress.
	/// </summary>
	bool IsRefreshing { get; }

	/// <summary>
	///     Gets the UTC timestamp of the last successful refresh.
	/// </summary>
	DateTimeOffset? LastRefreshUtc { get; }

	/// <summary>
	///     Gets the last error message encountered during operations.
	/// </summary>
	string? LastErrorMessage { get; }

	/// <summary>
	///     Gets the command to refresh the workshop explorer tree.
	/// </summary>
	IRelayCommand RefreshCommand { get; }

	/// <summary>
	///     Gets the command invoked when a tree node is selected.
	/// </summary>
	IRelayCommand<RoutedPropertyChangedEventArgs<object>> SelectNodeCommand { get; }

	/// <summary>
	///     Gets the command to create a new JSON file.
	/// </summary>
	IRelayCommand<WorkshopExplorerNodeModel?> CreateFileCommand { get; }

	/// <summary>
	///     Gets the command to delete a selected file.
	/// </summary>
	IAsyncRelayCommand<WorkshopExplorerNodeModel?> DeleteFileCommand { get; }

	/// <summary>
	///     Occurs when a file is selected in the explorer.
	/// </summary>
	/// <remarks>
	///     This event is raised when a user selects a JSON file in the tree.
	///     Subscribers can handle this event to load the file into an editor.
	/// </remarks>
	event EventHandler<EditorDocumentReference>? FileSelected;
}