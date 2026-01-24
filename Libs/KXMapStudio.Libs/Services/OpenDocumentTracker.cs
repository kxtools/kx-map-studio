namespace KXMapStudio.Libs.Services;

/// <summary>
///     Tracks the currently open document by observing the grid editor.
/// </summary>
/// <remarks>
///     This service acts as a lightweight facade over <see cref="IGridEditorViewModel" />,
///     exposing only the minimal information needed to track which file is currently open.
///     This maintains separation of concerns and reduces coupling between components.
/// </remarks>
public sealed class OpenDocumentTracker : IOpenDocumentTracker
{
	private readonly IGridEditorViewModel _gridEditor;

	/// <summary>
	///     Initializes a new instance of the <see cref="OpenDocumentTracker" /> class.
	/// </summary>
	/// <param name="gridEditor">The grid editor ViewModel to observe.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="gridEditor" /> is <see langword="null" />.</exception>
	public OpenDocumentTracker(IGridEditorViewModel gridEditor)
	{
		ArgumentNullException.ThrowIfNull(gridEditor);
		_gridEditor = gridEditor;
	}

	/// <summary>
	///     Gets the full path of the currently open document, or <see langword="null" /> if no document is open.
	/// </summary>
	public string? CurrentOpenFilePath => _gridEditor.OpenedFilePath;

	/// <summary>
	///     Gets a value indicating whether a document is currently loaded.
	/// </summary>
	public bool HasOpenDocument => _gridEditor.IsLoaded;

	/// <summary>
	///     Checks if the specified file path is currently open.
	/// </summary>
	/// <param name="filePath">The file path to check.</param>
	/// <returns><see langword="true" /> if the file is currently open; otherwise, <see langword="false" />.</returns>
	public bool IsFileOpen(string filePath)
	{
		return HasOpenDocument &&
		       !string.IsNullOrEmpty(CurrentOpenFilePath) &&
		       string.Equals(CurrentOpenFilePath, filePath, StringComparison.OrdinalIgnoreCase);
	}
}