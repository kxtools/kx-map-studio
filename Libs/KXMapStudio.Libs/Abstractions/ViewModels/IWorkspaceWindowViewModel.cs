namespace KXMapStudio.Libs.Abstractions.ViewModels;

/// <summary>
///     Defines the contract for the root workspace window ViewModel.
/// </summary>
/// <remarks>
///     This interface aggregates child ViewModels (left panel, grid editor, status bar)
///     and serves as the DataContext for the main workspace window.
/// </remarks>
public interface IWorkspaceWindowViewModel
{
	/// <summary>
	///     Gets the left panel ViewModel managing file exploration and preview.
	/// </summary>
	ILeftPanelViewModel LeftPanel { get; }

	/// <summary>
	///     Gets the grid editor ViewModel managing coordinate editing.
	/// </summary>
	IGridEditorViewModel GridEditor { get; }

	/// <summary>
	///     Gets the status bar ViewModel displaying application state.
	/// </summary>
	IStatusBarViewModel StatusBar { get; }

	/// <summary>
	///     Gets the message queue for displaying snackbar notifications.
	/// </summary>
	ISnackbarMessageQueue SnackbarMessageQueue { get; }
}