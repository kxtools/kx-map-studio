namespace KXMapStudio.Libs.Abstractions.ViewModels.LeftSide;

/// <summary>
///     Defines the contract for the left panel ViewModel.
/// </summary>
/// <remarks>
///     <para>
///         This ViewModel orchestrates the left side of the workspace,
///         containing file exploration and grid editor components.
///     </para>
///     <para>
///         The left panel acts as a mediator, coordinating communication between
///         the workshop explorer and the grid editor via event subscriptions.
///     </para>
/// </remarks>
public interface ILeftPanelViewModel : IDisposable
{
	/// <summary>
	///     Gets the workshop explorer ViewModel responsible for file tree navigation.
	/// </summary>
	IWorkshopExplorerViewModel WorkshopExplorer { get; }

	/// <summary>
	///     Gets the grid editor ViewModel responsible for coordinate editing.
	/// </summary>
	IGridEditorViewModel GridEditor { get; }
}