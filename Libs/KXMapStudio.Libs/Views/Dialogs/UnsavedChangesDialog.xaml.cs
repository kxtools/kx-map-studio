namespace KXMapStudio.Libs.Views.Dialogs;

/// <summary>
///     Dialog window for confirming actions when there are unsaved changes.
/// </summary>
public partial class UnsavedChangesDialog : Window
{
	/// <summary>
	///     Initializes a new instance of the <see cref="UnsavedChangesDialog" /> class.
	/// </summary>
	/// <param name="fileName">The name of the file with unsaved changes.</param>
	public UnsavedChangesDialog(string fileName)
	{
		InitializeComponent();

		// Set the owner to the main window if possible
		if (Application.Current.MainWindow != null &&
		    Application.Current.MainWindow != this)
			Owner = Application.Current.MainWindow;

		MessageTextBlock.Text = $"The file '{fileName}' has unsaved changes. " +
		                        "Do you want to save your changes before continuing?";
	}

	/// <summary>
	///     Gets the result of the dialog interaction.
	/// </summary>
	public UnsavedChangesDialogResult Result { get; private set; } = UnsavedChangesDialogResult.Cancel;

	private void SaveAndContinueButton_Click(object sender, RoutedEventArgs e)
	{
		Result = UnsavedChangesDialogResult.SaveAndContinue;
		DialogResult = true;
		Close();
	}

	private void ContinueWithoutSavingButton_Click(object sender, RoutedEventArgs e)
	{
		Result = UnsavedChangesDialogResult.ContinueWithoutSaving;
		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Result = UnsavedChangesDialogResult.Cancel;
		DialogResult = false;
		Close();
	}
}