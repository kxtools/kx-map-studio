namespace KXMapStudio.Core.Types.Enums;

/// <summary>
///     Represents the connection state of the MumbleLink service.
/// </summary>
public enum MumbleConnectionState : byte
{
	/// <summary>
	///     Not connected - MumbleLink is not available or GW2 is not running.
	/// </summary>
	Disconnected = 0,

	/// <summary>
	///     Stale connection - MumbleLink is available but no movement detected (AFK, menu, loading).
	/// </summary>
	Stale = 1,

	/// <summary>
	///     Connected - MumbleLink is active and player movement is detected.
	/// </summary>
	Connected = 2
}