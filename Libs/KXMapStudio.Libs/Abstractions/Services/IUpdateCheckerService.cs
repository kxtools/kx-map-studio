namespace KXMapStudio.Libs.Abstractions.Services;

/// <summary>
///     Defines the contract for checking application updates from GitHub.
/// </summary>
/// <remarks>
///     This service provides version checking functionality to notify users when
///     a newer version of the application is available on GitHub.
/// </remarks>
public interface IUpdateCheckerService
{
	/// <summary>
	///     Gets a value indicating whether an update is available.
	/// </summary>
	bool IsUpdateAvailable { get; }

	/// <summary>
	///     Gets the URL of the latest release on GitHub.
	/// </summary>
	/// <remarks>
	///     This value is only meaningful when <see cref="IsUpdateAvailable" /> is <see langword="true" />.
	/// </remarks>
	string? LatestVersionUrl { get; }

	/// <summary>
	///     Gets the tag name of the latest version (e.g., "v1.2.3").
	/// </summary>
	/// <remarks>
	///     This value is only meaningful when <see cref="IsUpdateAvailable" /> is <see langword="true" />.
	/// </remarks>
	string? LatestVersionTag { get; }

	/// <summary>
	///     Asynchronously checks for updates from GitHub.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	///     This method updates the <see cref="IsUpdateAvailable" />, <see cref="LatestVersionUrl" />,
	///     and <see cref="LatestVersionTag" /> properties based on the latest release information from GitHub.
	///     If the check fails (network error, invalid response), the properties remain unchanged.
	/// </remarks>
	Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}