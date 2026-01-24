namespace KXMapStudio.Libs.Abstractions.Services.GridEditor;

/// <summary>
///     Defines the contract for grid editor document operations (load, save, save as).
/// </summary>
/// <remarks>
///     This service works with data models (<see cref="GridRowData" />) rather than ViewModels
///     to maintain separation of concerns and enable service reusability.
/// </remarks>
public interface IGridEditorDocumentService
{
	/// <summary>
	///     Asynchronously loads a document and returns coordinate data.
	/// </summary>
	/// <param name="doc">The document reference to load.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>
	///     A tuple containing the loaded coordinate data and original file bytes.
	/// </returns>
	Task<(IReadOnlyList<GridRowData> rows, byte[] originalBytes)> LoadAsync(
		EditorDocumentReference doc,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Asynchronously saves a document to its original location.
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="rows">The coordinate data to save.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	Task SaveAsync(
		EditorDocumentReference doc,
		IReadOnlyList<GridRowData> rows,
		CancellationToken cancellationToken = default);

	/// <summary>
	///     Asynchronously saves a document to a user-specified location (Save As).
	/// </summary>
	/// <param name="doc">The document reference.</param>
	/// <param name="rows">The coordinate data to save.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	Task SaveAsAsync(
		EditorDocumentReference doc,
		IReadOnlyList<GridRowData> rows,
		CancellationToken cancellationToken = default);
}