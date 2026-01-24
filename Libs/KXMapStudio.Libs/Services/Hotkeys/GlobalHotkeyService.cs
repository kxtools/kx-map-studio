namespace KXMapStudio.Libs.Services.Hotkeys;

/// <summary>
///     Provides global hotkey registration using Windows API.
///     Implements F9 hotkey for adding markers from Mumble coordinates.
/// </summary>
/// <remarks>
///     <para>
///         This implementation uses low-level Windows API (<c>RegisterHotKey</c>/<c>UnregisterHotKey</c>)
///         to register system-wide hotkeys that work even when the application is in the background.
///     </para>
///     <para>
///         The service is defensive and handles registration failures gracefully (e.g., when the hotkey
///         is already in use by another application).
///     </para>
///     <para>
///         Implements <see cref="IDisposable" /> to ensure proper cleanup of hotkey registrations.
///     </para>
/// </remarks>
public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
	private const int WmHotkey = 0x0312;
	private const int HotkeyIdAddMarker = 1;

	// Virtual-Key Codes
	private const uint VkF9 = 0x78;

	// Modifiers
	private const uint ModNone = 0x0000;

	private readonly ILogger<GlobalHotkeyService> _logger;
	private bool _disposed;

	private nint _hwnd;
	private HwndSource? _hwndSource;
	private bool _isRegistered;

	/// <summary>
	///     Initializes a new instance of the <see cref="GlobalHotkeyService" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic and error tracking.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger" /> is <see langword="null" />.</exception>
	public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);
		_logger = logger;
	}

	/// <summary>
	///     Occurs when the "Add Marker from Mumble" hotkey (F9) is pressed.
	/// </summary>
	public event EventHandler? AddMarkerFromMumblePressed;

	/// <summary>
	///     Initializes the hotkey service with a window handle for message processing.
	/// </summary>
	/// <param name="window">The WPF window to attach the hotkey handler to.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="window" /> is <see langword="null" />.</exception>
	/// <remarks>
	///     If the window is not yet shown, the service hooks into <see cref="Window.SourceInitialized" />
	///     to defer initialization until the native handle is available.
	/// </remarks>
	public void Initialize(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);

		if (_hwndSource is not null)
		{
			_logger.LogWarning("GlobalHotkeyService is already initialized. Ignoring re-initialization.");
			return;
		}

		var helper = new WindowInteropHelper(window);
		_hwnd = helper.Handle;

		if (_hwnd == nint.Zero)
		{
			_logger.LogDebug("Window handle not yet available. Hooking into SourceInitialized event.");

			// Window not yet shown, hook into SourceInitialized
			window.SourceInitialized += (_, _) =>
			{
				_hwnd = new WindowInteropHelper(window).Handle;
				_hwndSource = HwndSource.FromHwnd(_hwnd);
				_hwndSource?.AddHook(WndProc);

				_logger.LogDebug("Window handle acquired: {Hwnd}. Registering hotkeys.", _hwnd);
				RegisterHotkeys();
			};
		}
		else
		{
			_logger.LogDebug("Window handle available immediately: {Hwnd}.", _hwnd);
			_hwndSource = HwndSource.FromHwnd(_hwnd);
			_hwndSource?.AddHook(WndProc);
		}
	}

	/// <summary>
	///     Registers all configured hotkeys with the Windows system.
	/// </summary>
	/// <remarks>
	///     This method is idempotent - calling it multiple times when already registered has no effect.
	///     Registration failures are logged but do not throw exceptions.
	/// </remarks>
	public void RegisterHotkeys()
	{
		if (_hwnd == nint.Zero)
		{
			_logger.LogWarning("Cannot register hotkeys: Window handle is not available.");
			return;
		}

		if (_isRegistered)
		{
			_logger.LogDebug("Hotkeys are already registered. Skipping registration.");
			return;
		}

		try
		{
			var registered = RegisterHotKey(_hwnd, HotkeyIdAddMarker, ModNone, VkF9);
			if (registered)
			{
				_isRegistered = true;
				_logger.LogInformation("F9 hotkey registered successfully.");
			}
			else
			{
				_logger.LogWarning(
					"Failed to register F9 hotkey. It may be in use by another application. Last Win32 Error: {Error}",
					Marshal.GetLastWin32Error());
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Exception occurred during hotkey registration.");
		}
	}

	/// <summary>
	///     Unregisters all hotkeys from the Windows system.
	/// </summary>
	/// <remarks>
	///     This method is idempotent - calling it when no hotkeys are registered has no effect.
	/// </remarks>
	public void UnregisterHotkeys()
	{
		if (_hwnd == nint.Zero || !_isRegistered)
		{
			_logger.LogDebug("No hotkeys to unregister.");
			return;
		}

		try
		{
			UnregisterHotKey(_hwnd, HotkeyIdAddMarker);
			_isRegistered = false;
			_logger.LogInformation("F9 hotkey unregistered successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Exception occurred during hotkey unregistration.");
		}
	}

	/// <summary>
	///     Disposes resources and unregisters all hotkeys.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_logger.LogDebug("Disposing GlobalHotkeyService.");

		UnregisterHotkeys();

		if (_hwndSource is not null)
		{
			_hwndSource.RemoveHook(WndProc);
			_hwndSource = null;
			_logger.LogDebug("Removed window message hook.");
		}

		_disposed = true;
		GC.SuppressFinalize(this);
		_logger.LogInformation("GlobalHotkeyService disposed successfully.");
	}

	/// <summary>
	///     Windows message procedure callback for processing hotkey messages.
	/// </summary>
	/// <param name="hwnd">The window handle.</param>
	/// <param name="msg">The message identifier.</param>
	/// <param name="wParam">Message-specific parameter (hotkey ID).</param>
	/// <param name="lParam">Message-specific parameter (key combination).</param>
	/// <param name="handled">Output parameter indicating whether the message was handled.</param>
	/// <returns>Always returns <see cref="nint.Zero" />.</returns>
	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == WmHotkey)
		{
			var hotkeyId = wParam.ToInt32();
			if (hotkeyId == HotkeyIdAddMarker)
			{
				_logger.LogDebug("F9 hotkey pressed. Raising AddMarkerFromMumblePressed event.");
				AddMarkerFromMumblePressed?.Invoke(this, EventArgs.Empty);
				handled = true;
			}
		}

		return nint.Zero;
	}

	#region Windows API

	/// <summary>
	///     Registers a system-wide hotkey.
	/// </summary>
	/// <param name="hWnd">Handle to the window that will receive hotkey messages.</param>
	/// <param name="id">Unique hotkey identifier.</param>
	/// <param name="fsModifiers">Modifier keys (e.g., Ctrl, Alt, Shift).</param>
	/// <param name="vk">Virtual key code of the hotkey.</param>
	/// <returns><see langword="true" /> if registration succeeded; otherwise, <see langword="false" />.</returns>
	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	/// <summary>
	///     Unregisters a system-wide hotkey.
	/// </summary>
	/// <param name="hWnd">Handle to the window associated with the hotkey.</param>
	/// <param name="id">Unique hotkey identifier.</param>
	/// <returns><see langword="true" /> if unregistration succeeded; otherwise, <see langword="false" />.</returns>
	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnregisterHotKey(nint hWnd, int id);

	#endregion
}