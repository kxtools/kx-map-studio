namespace KXMapStudio.Libs.Services.Dialogs;

/// <summary>
///     Service for displaying file dialogs (save, create, delete confirmation) in WPF applications.
/// </summary>
/// <remarks>
///     This service wraps WPF dialog interactions to provide testable abstractions and centralized logging.
///     All methods execute synchronously on the UI thread but return <see cref="Task{T}" /> for API consistency.
/// </remarks>
public sealed class SaveFileDialogService : ISaveFileDialogService
{
	private readonly ILogger<SaveFileDialogService> _logger;

	/// <summary>
	///     Initializes a new instance of the <see cref="SaveFileDialogService" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger" /> is <see langword="null" />.</exception>
	public SaveFileDialogService(ILogger<SaveFileDialogService> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);
		_logger = logger;
	}

	/// <summary>
	///     Displays a Save File Dialog for JSON files.
	/// </summary>
	/// <param name="suggestedFileName">The suggested filename to display in the dialog (optional).</param>
	/// <param name="cancellationToken">A cancellation token (currently unused, for API future-proofing).</param>
	/// <returns>
	///     A <see cref="Task{T}" /> representing the operation, containing the selected file path,
	///     or <see langword="null" /> if the user canceled the dialog.
	/// </returns>
	public Task<string?> ShowSaveJsonAsync(string? suggestedFileName, CancellationToken cancellationToken = default)
	{
		// Note: CancellationToken currently unused (dialogs are synchronous).
		// Reserved for future async dialog support when WPF provides native async APIs.
		_ = cancellationToken;

		_logger.LogDebug("Displaying Save JSON dialog with suggested filename: {SuggestedFileName}",
			suggestedFileName ?? "document.json");

		var dialog = new SaveFileDialog
		{
			Title = "Save JSON As",
			Filter = "JSON files (*.json)|*.json",
			FileName = string.IsNullOrWhiteSpace(suggestedFileName) ? "document.json" : suggestedFileName
		};

		var result = dialog.ShowDialog() == true ? dialog.FileName : null;

		if (result is not null)
			_logger.LogInformation("User selected file path: {FilePath}", result);
		else
			_logger.LogDebug("User canceled Save JSON dialog.");

		return Task.FromResult(result);
	}

	/// <summary>
	///     Displays a Create New File Dialog for JSON files in a specific folder.
	/// </summary>
	/// <param name="defaultFolder">The initial directory to display in the dialog.</param>
	/// <param name="cancellationToken">A cancellation token (currently unused, for API future-proofing).</param>
	/// <returns>
	///     A <see cref="Task{T}" /> representing the operation, containing the selected file path,
	///     or <see langword="null" /> if the user canceled the dialog.
	/// </returns>
	public Task<string?> ShowCreateJsonFileDialogAsync(string defaultFolder,
		CancellationToken cancellationToken = default)
	{
		// Note: CancellationToken currently unused (dialogs are synchronous).
		// Reserved for future async dialog support when WPF provides native async APIs.
		_ = cancellationToken;

		_logger.LogDebug("Displaying Create JSON File dialog in folder: {DefaultFolder}", defaultFolder);

		var dialog = new SaveFileDialog
		{
			Title = "Create New JSON File",
			Filter = "JSON files (*.json)|*.json",
			FileName = "new_file.json",
			InitialDirectory = Directory.Exists(defaultFolder) ? defaultFolder : null
		};

		var result = dialog.ShowDialog() == true ? dialog.FileName : null;

		if (result is not null)
			_logger.LogInformation("User created new file: {FilePath}", result);
		else
			_logger.LogDebug("User canceled Create JSON File dialog.");

		return Task.FromResult(result);
	}

	/// <summary>
	///     Displays a confirmation dialog for file deletion.
	/// </summary>
	/// <param name="fileName">The name of the file to delete (displayed in the confirmation message).</param>
	/// <returns>
	///     A <see cref="Task{T}" /> representing the operation, containing <see langword="true" /> if the user confirmed
	///     deletion;
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="fileName" /> is null or whitespace.</exception>
	public Task<bool> ShowDeleteFileConfirmationAsync(string fileName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		_logger.LogDebug("Displaying delete confirmation for file: {FileName}", fileName);

		var result = MessageBox.Show(
			$"Are you sure you want to delete '{fileName}'?\n\nThis action cannot be undone.",
			"Confirm Delete",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning,
			MessageBoxResult.No);

		var confirmed = result == MessageBoxResult.Yes;

		if (confirmed)
			_logger.LogInformation("User confirmed deletion of file: {FileName}", fileName);
		else
			_logger.LogDebug("User canceled deletion of file: {FileName}", fileName);

		return Task.FromResult(confirmed);
	}

	/// <summary>
	///     Displays a confirmation dialog when attempting to switch files with unsaved changes.
	/// </summary>
	/// <param name="fileName">The name of the file with unsaved changes.</param>
	/// <returns>
	///     A <see cref="Task{T}" /> representing the operation, containing the user's choice
	///     (<see cref="UnsavedChangesDialogResult" />).
	/// </returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="fileName" /> is null or whitespace.</exception>
	public Task<UnsavedChangesDialogResult> ShowUnsavedChangesDialogAsync(string fileName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		_logger.LogDebug("Displaying unsaved changes dialog for file: {FileName}", fileName);

		var dialog = new UnsavedChangesDialog(fileName);
		dialog.ShowDialog();

		// Use the dialog's Result property which correctly captures the user's choice
		var dialogResult = dialog.Result;

		_logger.LogInformation("User chose '{Result}' for unsaved changes in file: {FileName}",
			dialogResult, fileName);

		return Task.FromResult(dialogResult);
	}
}