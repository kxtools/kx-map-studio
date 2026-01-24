namespace KXMapStudio.Core.Abstractions.Services.Markers;

/// <summary>
///     Defines the contract for creating marker coordinates from Mumble (Guild Wars 2) player position data.
/// </summary>
/// <remarks>
///     This service encapsulates the business logic for converting MumbleLink player position
///     into marker coordinate data suitable for grid storage. It belongs in the Core layer
///     as it represents a domain transformation rule.
/// </remarks>
public interface IMumbleMarkerService
{
	/// <summary>
	///     Creates a marker name using an auto-incrementing counter.
	/// </summary>
	/// <param name="counter">The current marker counter value.</param>
	/// <returns>A formatted marker name (e.g., "Marker 1", "Marker 2").</returns>
	string GenerateMarkerName(int counter);

	/// <summary>
	///     Validates whether a Mumble state is suitable for marker creation.
	/// </summary>
	/// <param name="mumbleState">The Mumble state to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the state is valid for marker creation;
	///     otherwise, <see langword="false" />.
	/// </returns>
	bool CanCreateMarkerFromMumbleState(MumbleStateModel mumbleState);

	/// <summary>
	///     Extracts coordinate values from a Mumble state for marker creation.
	/// </summary>
	/// <param name="mumbleState">The Mumble state containing player position.</param>
	/// <returns>A tuple containing the X, Y, and Z coordinates.</returns>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="mumbleState" /> is <see langword="null" />.
	/// </exception>
	(double X, double Y, double Z) ExtractCoordinatesFromMumble(MumbleStateModel mumbleState);
}