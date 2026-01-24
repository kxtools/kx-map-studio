namespace KXMapStudio.Libs.Services;

/// <summary>
///     Service for displaying non-intrusive notifications using Material Design Snackbar.
/// </summary>
/// <remarks>
///     This service wraps the Material Design SnackbarMessageQueue to provide
///     a centralized, testable interface for displaying transient notifications.
/// </remarks>
public sealed class NotificationService : INotificationService
{
	private readonly ILogger<NotificationService> _logger;
	private readonly ISnackbarMessageQueue _messageQueue;

	/// <summary>
	///     Initializes a new instance of the <see cref="NotificationService" /> class.
	/// </summary>
	/// <param name="messageQueue">The Material Design message queue for displaying snackbar notifications.</param>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	/// <exception cref="ArgumentNullException">
	///     Thrown when <paramref name="messageQueue" /> or <paramref name="logger" /> is <see langword="null" />.
	/// </exception>
	public NotificationService(ISnackbarMessageQueue messageQueue, ILogger<NotificationService> logger)
	{
		ArgumentNullException.ThrowIfNull(messageQueue);
		ArgumentNullException.ThrowIfNull(logger);

		_messageQueue = messageQueue;
		_logger = logger;
	}

	/// <summary>
	///     Displays an informational notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	public void ShowInfo(string message)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		_logger.LogDebug("Showing info notification: {Message}", message);
		_messageQueue.Enqueue(message);
	}

	/// <summary>
	///     Displays a success notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	public void ShowSuccess(string message)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		_logger.LogDebug("Showing success notification: {Message}", message);
		_messageQueue.Enqueue(message);
	}

	/// <summary>
	///     Displays a warning notification.
	/// </summary>
	/// <param name="message">The message to display.</param>
	public void ShowWarning(string message)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		_logger.LogDebug("Showing warning notification: {Message}", message);
		_messageQueue.Enqueue(message);
	}
}