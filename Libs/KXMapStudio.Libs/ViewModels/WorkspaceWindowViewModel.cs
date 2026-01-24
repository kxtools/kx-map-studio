namespace KXMapStudio.Libs.ViewModels;

/// <summary>
///     Root ViewModel for the workspace window, coordinating left panel, grid editor, and status bar ViewModels.
/// </summary>
/// <param name="leftPanel">The left panel ViewModel managing file exploration and preview.</param>
/// <param name="gridEditor">The grid editor ViewModel managing coordinate editing.</param>
/// <param name="statusBar">The status bar ViewModel displaying Mumble state and application status.</param>
/// <param name="snackbarMessageQueue">The message queue for displaying snackbar notifications.</param>
/// <remarks>
///     This ViewModel acts as the composition root for the main workspace UI,
///     aggregating child ViewModels without additional orchestration logic.
///     All child ViewModels are provided via dependency injection as singletons.
/// </remarks>
/// <exception cref="ArgumentNullException">
///     Thrown when any of the constructor parameters (<paramref name="leftPanel" />,
///     <paramref name="gridEditor" />, <paramref name="statusBar" />, or <paramref name="snackbarMessageQueue" />) is
///     <see langword="null" />.
/// </exception>
public sealed class WorkspaceWindowViewModel(
	ILeftPanelViewModel leftPanel,
	IGridEditorViewModel gridEditor,
	IStatusBarViewModel statusBar,
	ISnackbarMessageQueue snackbarMessageQueue)
	: ObservableObject, IWorkspaceWindowViewModel
{
	/// <summary>
	///     Gets the left panel ViewModel responsible for file exploration and selection.
	/// </summary>
	public ILeftPanelViewModel LeftPanel { get; } = leftPanel ?? throw new ArgumentNullException(nameof(leftPanel));

	/// <summary>
	///     Gets the grid editor ViewModel responsible for coordinate editing and document management.
	/// </summary>
	public IGridEditorViewModel GridEditor { get; } = gridEditor ?? throw new ArgumentNullException(nameof(gridEditor));

	/// <summary>
	///     Gets the status bar ViewModel displaying Mumble connection state and player information.
	/// </summary>
	public IStatusBarViewModel StatusBar { get; } = statusBar ?? throw new ArgumentNullException(nameof(statusBar));

	/// <summary>
	///     Gets the message queue for displaying snackbar notifications.
	/// </summary>
	public ISnackbarMessageQueue SnackbarMessageQueue { get; } =
		snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));
}