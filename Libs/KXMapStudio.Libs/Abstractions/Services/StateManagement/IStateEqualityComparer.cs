namespace KXMapStudio.Libs.Abstractions.Services.StateManagement;

/// <summary>
///     Defines a comparer for determining equality between two states.
/// </summary>
/// <typeparam name="TState">The type of state to compare.</typeparam>
/// <remarks>
///     This abstraction allows for custom equality logic specific to different state types,
///     enabling the state management service to be truly generic and reusable.
/// </remarks>
public interface IStateEqualityComparer<in TState>
{
	/// <summary>
	///     Determines whether two state instances are equal.
	/// </summary>
	/// <param name="left">The first state to compare.</param>
	/// <param name="right">The second state to compare.</param>
	/// <returns>
	///     <see langword="true" /> if the specified states are equal; otherwise, <see langword="false" />.
	/// </returns>
	bool Equals(TState? left, TState? right);
}