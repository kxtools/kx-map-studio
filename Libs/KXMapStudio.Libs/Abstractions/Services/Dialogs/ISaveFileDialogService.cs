namespace KXMapStudio.Libs.Abstractions.Services.Dialogs;

/// <summary>
///     Defines the contract for displaying file dialogs in WPF applications.
/// </summary>
/// <remarks>
///     <para>
///         This service abstracts WPF dialog interactions (SaveFileDialog, MessageBox)
///         to provide testable interfaces and centralized dialog management.
///     </para>
///     <para>
///         All methods execute synchronously on the UI thread but return <see cref="Task{T}" />
///         for API consistency and future async support.
///     </para>
/// </remarks>
public interface ISaveFileDialogService
{
	/// <summary>
	///     Displays a Save File Dialog for JSON files.
	/// </summary>
	/// <param name="suggestedFileName">
	///     The suggested filename to display in the dialog.
	///     If <see langword="null" /> or empty, defaults to "document.json".
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token (currently unused, reserved for future async dialog support).
	/// </param>
	/// <returns>
	///     A task containing the selected file path, or <see langword="null" /> if the user canceled the dialog.
	/// </returns>
	Task<string?> ShowSaveJsonAsync(string? suggestedFileName, CancellationToken cancellationToken = default);

	/// <summary>
	///     Displays a Create New File Dialog for JSON files in a specific folder.
	/// </summary>
	/// <param name="defaultFolder">
	///     The initial directory to display in the dialog.
	///     If the directory doesn't exist, the dialog uses the system default location.
	/// </param>
	/// <param name="cancellationToken">
	///     A cancellation token (currently unused, reserved for future async dialog support).
	/// </param>
	/// <returns>
	///     A task containing the selected file path for the new file,
	///     or <see langword="null" /> if the user canceled the dialog.
	/// </returns>
	Task<string?> ShowCreateJsonFileDialogAsync(string defaultFolder, CancellationToken cancellationToken = default);

	/// <summary>
	///     Displays a confirmation dialog for file deletion.
	/// </summary>
	/// <param name="fileName">
	///     The name of the file to delete (displayed in the confirmation message).
	/// </param>
	/// <returns>
	///     A task containing <see langword="true" /> if the user confirmed deletion;
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <remarks>
	///     The dialog displays a warning icon and emphasizes that the action cannot be undone.
	/// </remarks>
	Task<bool> ShowDeleteFileConfirmationAsync(string fileName);

	/// <summary>
	///     Displays a confirmation dialog when attempting to switch files with unsaved changes.
	/// </summary>
	/// <param name="fileName">
	///     The name of the file with unsaved changes.
	/// </param>
	/// <returns>
	///     A task containing the user's choice:
	///     <list type="bullet">
	///         <item><see cref="UnsavedChangesDialogResult.SaveAndContinue" />: Save changes and proceed.</item>
	///         <item><see cref="UnsavedChangesDialogResult.ContinueWithoutSaving" />: Discard changes and proceed.</item>
	///         <item><see cref="UnsavedChangesDialogResult.Cancel" />: Cancel the operation.</item>
	///     </list>
	/// </returns>
	Task<UnsavedChangesDialogResult> ShowUnsavedChangesDialogAsync(string fileName);
}