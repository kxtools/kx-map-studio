namespace KXMapStudio.Core.Abstractions.Http;

/// <summary>
///     Defines HTTP operations against GitHub used by the application.
/// </summary>
public interface IGithubHttpClient
{
	/// <summary>
	///     Checks whether a newer version than the current assembly is available on GitHub.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns><see langword="true" /> when an update is available; otherwise, <see langword="false" />.</returns>
	Task<bool> CheckCurrentVersion(CancellationToken cancellationToken = default);

	/// <summary>
	///     Gets the latest release information from GitHub.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>
	///     The latest release information, or <see langword="null" /> if the request fails or no release is available.
	/// </returns>
	Task<GitHubReleaseModel?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);
}