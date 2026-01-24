namespace KXMapStudio.Libs.Services.GridEditor.RowManipulation;

/// <summary>
///     Provides grid row manipulation operations for coordinate editor ViewModels.
/// </summary>
/// <remarks>
///     This service extracts row manipulation logic from ViewModels, improving
///     testability and reducing ViewModel complexity.
/// </remarks>
public sealed class GridRowManipulationService : IGridRowManipulationService
{
	/// <inheritdoc />
	public void ReindexIds(IList<GridEditorRowViewModel> rows)
	{
		ArgumentNullException.ThrowIfNull(rows);

		for (var i = 0; i < rows.Count; i++)
			rows[i].Id = i + 1;
	}

	/// <inheritdoc />
	public bool CanMoveUp(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row)
	{
		if (row is null)
			return false;

		var index = GetRowIndex(rows, row);
		return index > 0;
	}

	/// <inheritdoc />
	public bool CanMoveDown(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row)
	{
		if (row is null)
			return false;

		var index = GetRowIndex(rows, row);
		return index >= 0 && index < rows.Count - 1;
	}

	/// <inheritdoc />
	public int GetRowIndex(IList<GridEditorRowViewModel> rows, GridEditorRowViewModel? row)
	{
		ArgumentNullException.ThrowIfNull(rows);

		if (row is null)
			return -1;

		return rows.IndexOf(row);
	}
}