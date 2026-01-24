namespace KXMapStudio.Libs.Services;

/// <summary>
///     Provides WPF Dispatcher invocation helpers for UI thread synchronization.
/// </summary>
/// <remarks>
///     This service uses the current application's dispatcher to execute actions on the UI thread.
///     All methods are thread-safe and can be called from any thread.
/// </remarks>
public sealed class DispatcherHelper : IDispatcherHelper
{
	private readonly ILogger<DispatcherHelper> _logger;

	/// <summary>
	///     Initializes a new instance of the <see cref="DispatcherHelper" /> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic tracking.</param>
	public DispatcherHelper(ILogger<DispatcherHelper> logger)
	{
		ArgumentNullException.ThrowIfNull(logger);
		_logger = logger;
	}

	/// <summary>
	///     Executes the specified action synchronously on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public void InvokeOnUIThread(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher is null)
		{
			_logger.LogWarning("Application dispatcher is not available. Executing action on current thread.");
			action();
			return;
		}

		if (dispatcher.CheckAccess())
			action();
		else
			dispatcher.Invoke(action);
	}

	/// <summary>
	///     Executes the specified action asynchronously on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task InvokeOnUIThreadAsync(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher is null)
		{
			_logger.LogWarning("Application dispatcher is not available. Executing action on current thread.");
			action();
			return;
		}

		if (dispatcher.CheckAccess())
			action();
		else
			await dispatcher.InvokeAsync(action);
	}

	/// <summary>
	///     Executes the specified async function on the UI thread.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="func">The async function to execute.</param>
	/// <returns>A task representing the asynchronous operation with result.</returns>
	public async Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> func)
	{
		ArgumentNullException.ThrowIfNull(func);

		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher is null)
		{
			_logger.LogWarning("Application dispatcher is not available. Executing function on current thread.");
			return await func();
		}

		if (dispatcher.CheckAccess()) return await func();

		return await dispatcher.InvokeAsync(func).Task.Unwrap();
	}
}