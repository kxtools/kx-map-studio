namespace KXMapStudio.Libs.Services.StateManagement;

/// <summary>
///     Provides equality comparison for collections of <see cref="GridEditorRowViewModel" />.
/// </summary>
/// <remarks>
///     This comparer performs deep value comparison of row properties (Name, X, Y, Z)
///     with floating-point tolerance for coordinate values.
/// </remarks>
public sealed class GridEditorRowEqualityComparer : IStateEqualityComparer<IReadOnlyList<GridEditorRowViewModel>>
{
	private const double Tolerance = 0.0001;
	private readonly ILogger<GridEditorRowEqualityComparer> _logger;

	/// <summary>
	///     Initializes a new instance of the <see cref="GridEditorRowEqualityComparer" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	public GridEditorRowEqualityComparer(ILogger<GridEditorRowEqualityComparer> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);
		_logger = logger;
	}

	/// <summary>
	///     Determines whether two collections of grid rows are equal.
	/// </summary>
	/// <param name="left">The first collection to compare.</param>
	/// <param name="right">The second collection to compare.</param>
	/// <returns>
	///     <see langword="true" /> if both collections have the same count and all rows match; otherwise,
	///     <see langword="false" />.
	/// </returns>
	public bool Equals(IReadOnlyList<GridEditorRowViewModel>? left, IReadOnlyList<GridEditorRowViewModel>? right)
	{
		if (ReferenceEquals(left, right))
			return true;

		if (left is null || right is null)
		{
			_logger.LogTrace("One of the collections is null. Not equal.");
			return false;
		}

		if (left.Count != right.Count)
		{
			_logger.LogTrace("Row count mismatch: {LeftCount} vs {RightCount}. Not equal.", left.Count, right.Count);
			return false;
		}

		for (var i = 0; i < left.Count; i++)
		{
			var leftRow = left[i];
			var rightRow = right[i];

			if (leftRow.Name == rightRow.Name
			    && !(Math.Abs(leftRow.X - rightRow.X) > Tolerance)
			    && !(Math.Abs(leftRow.Y - rightRow.Y) > Tolerance)
			    && !(Math.Abs(leftRow.Z - rightRow.Z) > Tolerance)) continue;
			_logger.LogTrace("Row {Index} differs. Not equal.", i);
			return false;
		}

		_logger.LogTrace("All rows match. Collections are equal.");
		return true;
	}
}