namespace KXMapStudio.Libs.Abstractions.Services.StateManagement;

/// <summary>
///     Maintains an in-memory undo/redo history for an arbitrary state object.
/// </summary>
/// <typeparam name="TState">Snapshot type to store (should be treated as immutable by callers).</typeparam>
public interface IStateManagementService<TState>
{
	/// <summary>
	///     Maximum number of undo snapshots retained.
	/// </summary>
	int Capacity { get; }

	bool CanUndo { get; }
	bool CanRedo { get; }

	/// <summary>
	///     Clears undo/redo history and original state.
	/// </summary>
	void Reset();

	/// <summary>
	///     Sets the original state snapshot for dirty tracking comparison.
	///     This is typically called after loading or saving a file.
	/// </summary>
	/// <param name="state">The state to mark as original (clean).</param>
	void SetOriginalState(TState state);

	/// <summary>
	///     Checks if the current state matches the original saved state.
	/// </summary>
	/// <param name="currentState">The current state to compare.</param>
	/// <returns>True if the current state equals the original state (file is clean).</returns>
	bool IsAtOriginalState(TState currentState);

	/// <summary>
	///     Pushes a snapshot representing the state <em>before</em> a modification.
	///     This clears the redo stack.
	/// </summary>
	void PushSnapshot(TState snapshot);

	/// <summary>
	///     Produces the state to apply for an Undo operation.
	/// </summary>
	/// <param name="current">Current state snapshot.</param>
	TState Undo(TState current);

	/// <summary>
	///     Produces the state to apply for a Redo operation.
	/// </summary>
	/// <param name="current">Current state snapshot.</param>
	TState Redo(TState current);

	event EventHandler? StateChanged;
}