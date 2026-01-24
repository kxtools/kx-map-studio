namespace KXMapStudio.Libs.Abstractions.ViewModels.BottomSide.StatusBar;

/// <summary>
///     Defines the contract for the status bar ViewModel.
/// </summary>
/// <remarks>
///     <para>
///         This ViewModel displays real-time Mumble state information including:
///         connection status, character name, map ID, and player coordinates.
///     </para>
///     <para>
///         This is a UI-facing abstraction (presentation layer).
///         It must remain free of WPF visual types to maintain testability.
///     </para>
/// </remarks>
public interface IStatusBarViewModel
{
	/// <summary>
	///     Gets the current Mumble connection state.
	/// </summary>
	/// <remarks>
	///     Possible values: <c>Disconnected</c>, <c>Connected</c>, <c>Stale</c> (AFK).
	/// </remarks>
	MumbleConnectionState ConnectionState { get; }

	/// <summary>
	///     Gets the character name from Mumble.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Displays "Not connected" when Mumble is disconnected.
	///     </para>
	///     <para>
	///         Appends "(AFK)" suffix when connection state is <c>Stale</c>.
	///     </para>
	/// </remarks>
	string CharacterName { get; }

	/// <summary>
	///     Gets the formatted map ID text.
	/// </summary>
	/// <remarks>
	///     Format: "Map: {mapId}" or "Map: N/A" when disconnected.
	/// </remarks>
	string MapText { get; }

	/// <summary>
	///     Gets the formatted coordinates text (all three axes).
	/// </summary>
	/// <remarks>
	///     Format: "Pos: {x}, {y}, {z}" with 2 decimal precision, or "Pos: N/A" when disconnected.
	/// </remarks>
	string CoordinatesText { get; }

	/// <summary>
	///     Gets the formatted X coordinate text.
	/// </summary>
	/// <remarks>
	///     Format: "X: {value}" with 2 decimal precision, or "X: -" when disconnected.
	/// </remarks>
	string XText { get; }

	/// <summary>
	///     Gets the formatted Y coordinate text.
	/// </summary>
	/// <remarks>
	///     Format: "Y: {value}" with 2 decimal precision, or "Y: -" when disconnected.
	/// </remarks>
	string YText { get; }

	/// <summary>
	///     Gets the formatted Z coordinate text.
	/// </summary>
	/// <remarks>
	///     Format: "Z: {value}" with 2 decimal precision, or "Z: -" when disconnected.
	/// </remarks>
	string ZText { get; }

	/// <summary>
	///     Gets a value indicating whether an application update is available.
	/// </summary>
	bool IsUpdateAvailable { get; }

	/// <summary>
	///     Gets the URL of the latest release on GitHub.
	/// </summary>
	string? LatestVersionUrl { get; }

	/// <summary>
	///     Gets the tag name of the latest version (e.g., "v1.2.3").
	/// </summary>
	string? LatestVersionTag { get; }
}