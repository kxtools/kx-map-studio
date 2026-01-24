namespace KXMapStudio.Core.Abstractions.Http;

/// <summary>
///     Defines HTTP access to Guild Wars 2 API payloads used by the application.
/// </summary>
public interface IGuildWarsHttpClient
{
	/// <summary>
	///     Retrieves continent floor data.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>The continent floor payload.</returns>
	Task<ContinentFloorModel> GetContinentsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Retrieves the full maps payload.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>A read-only list of maps.</returns>
	Task<IReadOnlyList<MapModel>> GetMapsAsync(CancellationToken cancellationToken = default);
}