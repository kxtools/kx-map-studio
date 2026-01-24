namespace KXMapStudio.Libs.Services;

/// <summary>
///     Provides a scoped mechanism to temporarily suppress dirty state tracking.
/// </summary>
/// <remarks>
///     <para>
///         This class implements <see cref="IDisposable" /> to provide RAII-style suppression scoping.
///         Use with a <c>using</c> statement to ensure proper restoration of dirty tracking.
///     </para>
///     <para>
///         This is a safer alternative to manual flag manipulation with <see cref="Interlocked" />.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// using (var suppression = new DirtyStateSuppression())
/// {
///     // Dirty tracking is suppressed within this scope
///     Rows.Add(newRow);
/// }
/// // Dirty tracking automatically restored here
/// </code>
/// </example>
public sealed class DirtyStateSuppression : IDisposable
{
	private readonly Action _restoreAction;
	private bool _disposed;

	/// <summary>
	///     Initializes a new instance of the <see cref="DirtyStateSuppression" /> class.
	/// </summary>
	/// <param name="suppressAction">The action to execute when suppression starts.</param>
	/// <param name="restoreAction">The action to execute when suppression ends (in Dispose).</param>
	public DirtyStateSuppression(Action suppressAction, Action restoreAction)
	{
		ArgumentNullException.ThrowIfNull(suppressAction);
		ArgumentNullException.ThrowIfNull(restoreAction);

		_restoreAction = restoreAction;
		suppressAction();
	}

	/// <summary>
	///     Restores dirty tracking when the scope exits.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_restoreAction();
		_disposed = true;
	}

	/// <summary>
	///     Creates a suppression scope using a ref int flag with thread-safe operations.
	/// </summary>
	/// <param name="suppressFlag">A reference to the suppression flag (0 = active, 1 = suppressed).</param>
	/// <returns>A disposable suppression scope.</returns>
	public static DirtyStateSuppression Create(ref int suppressFlag)
	{
		// Capture the reference in a local to avoid issues with ref in closures
		var flagRef = suppressFlag;

		return new DirtyStateSuppression(
			() => Interlocked.Exchange(ref flagRef, 1),
			() => Interlocked.Exchange(ref flagRef, 0)
		);
	}
}