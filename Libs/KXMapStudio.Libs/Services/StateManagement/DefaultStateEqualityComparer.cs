namespace KXMapStudio.Libs.Services.StateManagement;

/// <summary>
///     Default state equality comparer that uses <see cref="EqualityComparer{T}.Default" />.
/// </summary>
/// <typeparam name="TState">The type of state to compare.</typeparam>
/// <remarks>
///     This comparer is used as a fallback when no custom comparer is provided.
///     It delegates to the default equality comparer for the type.
/// </remarks>
internal sealed class DefaultStateEqualityComparer<TState> : IStateEqualityComparer<TState>
{
	/// <summary>
	///     Determines whether two state instances are equal using default equality comparison.
	/// </summary>
	/// <param name="left">The first state to compare.</param>
	/// <param name="right">The second state to compare.</param>
	/// <returns>
	///     <see langword="true" /> if the specified states are equal; otherwise, <see langword="false" />.
	/// </returns>
	public bool Equals(TState? left, TState? right)
	{
		return EqualityComparer<TState>.Default.Equals(left, right);
	}
}