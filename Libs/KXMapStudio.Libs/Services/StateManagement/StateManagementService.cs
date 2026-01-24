namespace KXMapStudio.Libs.Services.StateManagement;

/// <summary>
///     Implements undo/redo state management with dirty tracking for arbitrary state objects.
/// </summary>
/// <typeparam name="TState">The type of state to manage (should be treated as immutable by callers).</typeparam>
/// <remarks>
///     <para>
///         This service maintains two stacks (undo and redo) to enable unlimited undo/redo operations,
///         constrained by a configurable capacity to prevent excessive memory usage.
///     </para>
///     <para>
///         Supports intelligent dirty tracking by comparing the current state against an "original" baseline
///         (typically set after loading or saving a file).
///     </para>
///     <para>
///         Uses injectable <see cref="IStateEqualityComparer{TState}" /> for comparison logic,
///         making the service truly generic and reusable.
///     </para>
/// </remarks>
public sealed class StateManagementService<TState> : IStateManagementService<TState>
{
	private readonly IStateEqualityComparer<TState> _comparer;
	private readonly ILogger<StateManagementService<TState>> _logger;
	private readonly Stack<TState> _redo;
	private readonly Stack<TState> _undo;
	private TState? _originalState;

	/// <summary>
	///     Initializes a new instance of the <see cref="StateManagementService{TState}" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <param name="comparer">
	///     The equality comparer for state comparison. If <see langword="null" />,
	///     uses <see cref="EqualityComparer{T}.Default" />.
	/// </param>
	/// <param name="capacity">The maximum number of undo snapshots to retain (default: 20).</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger" /> is <see langword="null" />.</exception>
	public StateManagementService(
		ILogger<StateManagementService<TState>> logger,
		IStateEqualityComparer<TState>? comparer = null,
		int capacity = 20)
	{
		ArgumentNullException.ThrowIfNull(logger);

		_logger = logger;
		_comparer = comparer ?? new DefaultStateEqualityComparer<TState>();
		Capacity = capacity <= 0 ? 20 : capacity;

		_undo = new Stack<TState>(Capacity);
		_redo = new Stack<TState>(Capacity);

		_logger.LogDebug(
			"StateManagementService<{TypeName}> initialized with capacity: {Capacity}, comparer: {ComparerType}",
			typeof(TState).Name, Capacity, _comparer.GetType().Name);
	}

	/// <summary>
	///     Gets the maximum number of undo snapshots retained.
	/// </summary>
	public int Capacity { get; }

	/// <summary>
	///     Gets a value indicating whether an undo operation is available.
	/// </summary>
	public bool CanUndo => _undo.Count > 0;

	/// <summary>
	///     Gets a value indicating whether a redo operation is available.
	/// </summary>
	public bool CanRedo => _redo.Count > 0;

	/// <summary>
	///     Occurs when the state history changes (snapshot pushed, undo/redo performed, or reset).
	/// </summary>
	public event EventHandler? StateChanged;

	/// <summary>
	///     Sets the original state that will be used as baseline for dirty tracking.
	/// </summary>
	/// <param name="state">The state to mark as original (clean).</param>
	/// <remarks>
	///     This method is typically called after loading a file or saving changes to disk.
	/// </remarks>
	public void SetOriginalState(TState state)
	{
		_originalState = state;
		_logger.LogDebug("Original state set for dirty tracking.");
	}

	/// <summary>
	///     Checks if the current state matches the original saved state.
	/// </summary>
	/// <param name="currentState">The current state to compare.</param>
	/// <returns>
	///     <see langword="true" /> if the current state equals the original state (file is clean); otherwise,
	///     <see langword="false" />.
	/// </returns>
	/// <remarks>
	///     Uses the configured <see cref="IStateEqualityComparer{TState}" /> for comparison.
	/// </remarks>
	public bool IsAtOriginalState(TState currentState)
	{
		if (_originalState is null)
		{
			_logger.LogTrace("No original state set. Returning false for dirty check.");
			return false;
		}

		var isEqual = _comparer.Equals(_originalState, currentState);
		_logger.LogTrace("IsAtOriginalState check using {ComparerType}: {IsEqual}",
			_comparer.GetType().Name, isEqual);
		return isEqual;
	}

	/// <summary>
	///     Clears undo/redo history and original state.
	/// </summary>
	/// <remarks>
	///     This method is typically called when switching to a different document.
	/// </remarks>
	public void Reset()
	{
		_logger.LogDebug("Resetting state management. Clearing undo/redo stacks.");

		_undo.Clear();
		_redo.Clear();
		_originalState = default;

		StateChanged?.Invoke(this, EventArgs.Empty);
		_logger.LogInformation("State management reset successfully.");
	}

	/// <summary>
	///     Pushes a snapshot representing the state <strong>before</strong> a modification.
	///     This clears the redo stack.
	/// </summary>
	/// <param name="snapshot">The state snapshot to push.</param>
	/// <remarks>
	///     The undo stack is automatically trimmed to <see cref="Capacity" /> if it exceeds the limit.
	/// </remarks>
	public void PushSnapshot(TState snapshot)
	{
		_logger.LogDebug("Pushing snapshot to undo stack. Current undo count: {UndoCount}", _undo.Count);

		_undo.Push(snapshot);
		_redo.Clear();

		TrimUndoToCapacity();

		StateChanged?.Invoke(this, EventArgs.Empty);
		_logger.LogDebug("Snapshot pushed successfully. New undo count: {UndoCount}", _undo.Count);
	}

	/// <summary>
	///     Produces the state to apply for an Undo operation.
	/// </summary>
	/// <param name="current">Current state snapshot (pushed onto redo stack).</param>
	/// <returns>The previous state from the undo stack.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="CanUndo" /> is <see langword="false" />.</exception>
	public TState Undo(TState current)
	{
		if (!CanUndo)
		{
			_logger.LogWarning("Undo requested but no undo history available.");
			throw new InvalidOperationException("No undo history available.");
		}

		_logger.LogDebug("Performing undo. Current undo count: {UndoCount}", _undo.Count);

		_redo.Push(current);
		var previous = _undo.Pop();

		StateChanged?.Invoke(this, EventArgs.Empty);
		_logger.LogDebug("Undo completed. New undo count: {UndoCount}, Redo count: {RedoCount}",
			_undo.Count, _redo.Count);

		return previous;
	}

	/// <summary>
	///     Produces the state to apply for a Redo operation.
	/// </summary>
	/// <param name="current">Current state snapshot (pushed onto undo stack).</param>
	/// <returns>The next state from the redo stack.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <see cref="CanRedo" /> is <see langword="false" />.</exception>
	public TState Redo(TState current)
	{
		if (!CanRedo)
		{
			_logger.LogWarning("Redo requested but no redo history available.");
			throw new InvalidOperationException("No redo history available.");
		}

		_logger.LogDebug("Performing redo. Current redo count: {RedoCount}", _redo.Count);

		_undo.Push(current);
		TrimUndoToCapacity();

		var next = _redo.Pop();

		StateChanged?.Invoke(this, EventArgs.Empty);
		_logger.LogDebug("Redo completed. New undo count: {UndoCount}, Redo count: {RedoCount}",
			_undo.Count, _redo.Count);

		return next;
	}

	/// <summary>
	///     Trims the undo stack to the configured <see cref="Capacity" /> if it exceeds the limit.
	/// </summary>
	/// <remarks>
	///     Since <see cref="Stack{T}" /> doesn't support efficient trimming from the bottom,
	///     this method rebuilds the stack when necessary. The capacity is intentionally small (default 20),
	///     so the performance impact is negligible.
	/// </remarks>
	private void TrimUndoToCapacity()
	{
		if (_undo.Count <= Capacity)
			return;

		_logger.LogDebug("Trimming undo stack from {CurrentCount} to {Capacity}", _undo.Count, Capacity);

		// Rebuild stack, keeping only the most recent {Capacity} items
		var items = _undo.Reverse().ToArray(); // bottom -> top
		var kept = items.Skip(Math.Max(0, items.Length - Capacity)).ToArray();

		_undo.Clear();
		foreach (var item in kept)
			_undo.Push(item);

		_logger.LogDebug("Undo stack trimmed successfully. New count: {UndoCount}", _undo.Count);
	}
}