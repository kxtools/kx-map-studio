namespace KXMapStudio.Libs.Abstractions.Services.GridEditor.RowManipulation;

/// <summary>
///     Defines the contract for grid row manipulation operations.
/// </summary>
/// <remarks>
///     This service encapsulates row manipulation logic (move, reindex) that would
///     otherwise clutter the ViewModel, improving separation of concerns and testability.
/// </remarks>
public interface IGridRowManipulationService
{
	/// <summary>
	///     Reindexes all rows to ensure sequential 1-based IDs.
	/// </summary>
	/// <param name="rows">The collection of rows to reindex.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="rows" /> is <see langword="null" />.
	/// </exception>
	void ReindexIds(IList<GridEditorRowViewModel> rows);

	/// <summary>
	///     Validates whether a row can be moved up in the collection.
	/// </summary>
	/// <param name="rows">The collection containing the row.</param>
	/// <param name="row">The row to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the row can be moved up;
	///     otherwise, <see langword="false" />.
	/// </returns>
	bool CanMoveUp(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row);

	/// <summary>
	///     Validates whether a row can be moved down in the collection.
	/// </summary>
	/// <param name="rows">The collection containing the row.</param>
	/// <param name="row">The row to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the row can be moved down;
	///     otherwise, <see langword="false" />.
	/// </returns>
	bool CanMoveDown(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row);

	/// <summary>
	///     Gets the index of a row in the collection.
	/// </summary>
	/// <param name="rows">The collection to search.</param>
	/// <param name="row">The row to locate.</param>
	/// <returns>
	///     The zero-based index of the row, or -1 if not found.
	/// </returns>
	int GetRowIndex(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row);
}