namespace KXMapStudio.Core.Abstractions.Services.Mumble;

/// <summary>
///     Defines polling-based access to Guild Wars 2 MumbleLink state.
/// </summary>
public interface IMumbleService
{
	/// <summary>
	///     Gets the latest known Mumble state snapshot.
	/// </summary>
	MumbleStateModel Current { get; }

	/// <summary>
	///     Occurs when a new Mumble state snapshot is produced.
	/// </summary>
	event EventHandler<MumbleStateModel>? MumbleUpdated;

	/// <summary>
	///     Starts the background polling loop.
	/// </summary>
	void Start();

	/// <summary>
	///     Stops the background polling loop.
	/// </summary>
	void Stop();
}