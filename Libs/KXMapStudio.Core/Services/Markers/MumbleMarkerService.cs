namespace KXMapStudio.Core.Services.Markers;

/// <summary>
///     Provides business logic for creating marker coordinates from Mumble (Guild Wars 2) player position data.
/// </summary>
/// <remarks>
///     This service encapsulates domain rules for Mumble→Marker transformations,
///     keeping business logic in the Core layer separate from UI concerns.
/// </remarks>
public sealed class MumbleMarkerService : IMumbleMarkerService
{
	/// <inheritdoc />
	public string GenerateMarkerName(int counter)
	{
		return $"Marker {counter}";
	}

	/// <inheritdoc />
	public bool CanCreateMarkerFromMumbleState(MumbleStateModel mumbleState)
	{
		ArgumentNullException.ThrowIfNull(mumbleState);

		// Marker can be created if Mumble is available and connected
		return mumbleState is { IsAvailable: true, ConnectionState: MumbleConnectionState.Connected };
	}

	/// <inheritdoc />
	public (double X, double Y, double Z) ExtractCoordinatesFromMumble(MumbleStateModel mumbleState)
	{
		ArgumentNullException.ThrowIfNull(mumbleState);

		return (mumbleState.PlayerPosition.X, mumbleState.PlayerPosition.Y, mumbleState.PlayerPosition.Z);
	}
}