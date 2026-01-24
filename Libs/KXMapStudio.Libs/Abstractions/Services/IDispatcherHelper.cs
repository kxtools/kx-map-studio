namespace KXMapStudio.Libs.Abstractions.Services;

/// <summary>
///     Provides thread-safe WPF Dispatcher invocation helpers.
/// </summary>
/// <remarks>
///     This service centralizes UI thread synchronization logic,
///     eliminating duplicate Dispatcher.Invoke patterns across ViewModels.
/// </remarks>
public interface IDispatcherHelper
{
	/// <summary>
	///     Executes the specified action synchronously on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <remarks>
	///     If already on the UI thread, executes immediately.
	///     Otherwise, dispatches to the UI thread synchronously.
	/// </remarks>
	void InvokeOnUIThread(Action action);

	/// <summary>
	///     Executes the specified action asynchronously on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task InvokeOnUIThreadAsync(Action action);

	/// <summary>
	///     Executes the specified async function on the UI thread.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="func">The async function to execute.</param>
	/// <returns>A task representing the asynchronous operation with result.</returns>
	Task<T> InvokeOnUIThreadAsync<T>(Func<Task<T>> func);
}