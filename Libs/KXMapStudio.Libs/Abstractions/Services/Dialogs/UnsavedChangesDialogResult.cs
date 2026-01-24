namespace KXMapStudio.Libs.Abstractions.Services.Dialogs;

/// <summary>
///     Represents the user's choice in the unsaved changes dialog.
/// </summary>
public enum UnsavedChangesDialogResult
{
	/// <summary>
	///     Save the current document and continue with the operation.
	/// </summary>
	SaveAndContinue,

	/// <summary>
	///     Continue with the operation without saving changes (discard changes).
	/// </summary>
	ContinueWithoutSaving,

	/// <summary>
	///     Cancel the operation and stay on the current document.
	/// </summary>
	Cancel
}