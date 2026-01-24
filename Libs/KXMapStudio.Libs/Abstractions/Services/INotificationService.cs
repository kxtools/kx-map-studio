namespace KXMapStudio.Libs.Abstractions.Services;

/// <summary>
///     Defines the contract for displaying non-intrusive notifications to the user.
/// </summary>
/// <remarks>
///     <para>
///         This service provides a centralized mechanism for displaying transient notifications
///         such as success messages, informational alerts, and warnings using Material Design Snackbar.
///     </para>
///     <para>
///         Notifications are non-blocking and automatically dismiss after a short duration.
///     </para>
/// </remarks>
public interface INotificationService
{
	/// <summary>
	///     Displays an informational notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <remarks>
	///     Use this for general informational messages that don't require user action.
	/// </remarks>
	void ShowInfo(string message);

	/// <summary>
	///     Displays a success notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <remarks>
	///     Use this to confirm successful operations (e.g., "File successfully saved").
	/// </remarks>
	void ShowSuccess(string message);

	/// <summary>
	///     Displays a warning notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	/// <remarks>
	///     Use this for warnings that don't prevent operation but require user awareness.
	/// </remarks>
	void ShowWarning(string message);
}