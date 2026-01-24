namespace KXMapStudio.Libs.Abstractions.Services.Hotkeys;

/// <summary>
///     Provides global hotkey registration and management for the application.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
	/// <summary>
	///     Occurs when the "Add Marker from Mumble" hotkey (F9) is pressed.
	/// </summary>
	event EventHandler? AddMarkerFromMumblePressed;

	/// <summary>
	///     Initializes the hotkey service with a window handle for message processing.
	/// </summary>
	/// <param name="window">The WPF window to attach the hotkey handler to.</param>
	void Initialize(Window window);

	/// <summary>
	///     Registers all configured hotkeys with the system.
	/// </summary>
	void RegisterHotkeys();

	/// <summary>
	///     Unregisters all hotkeys from the system.
	/// </summary>
	void UnregisterHotkeys();
}