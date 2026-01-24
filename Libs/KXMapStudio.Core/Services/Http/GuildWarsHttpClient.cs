namespace KXMapStudio.Core.Services.Http;

/// <summary>
///     Provides access to Guild Wars 2 JSON payload endpoints used by KXMapStudio.
/// </summary>
/// <param name="HttpClient">The underlying HTTP client.</param>
public sealed record GuildWarsHttpClient(HttpClient HttpClient) : IGuildWarsHttpClient
{
	/// <summary>
	///     Retrieves the default continent floor payload.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>The deserialized <see cref="ContinentFloorModel" /> payload.</returns>
	/// <exception cref="HttpRequestException">Thrown when the HTTP call fails.</exception>
	/// <exception cref="JsonException">Thrown when the JSON payload is invalid.</exception>
	public async Task<ContinentFloorModel> GetContinentsAsync(CancellationToken cancellationToken = default)
	{
		var model = await HttpClient.GetFromJsonAsync<ContinentFloorModel>(
			Constants.GuildWars.ContinentsUrl,
			JsonRepository.DefaultJsonOptions,
			cancellationToken);

		return model ?? throw new JsonException("Guild Wars 2 continents payload was empty.");
	}

	/// <summary>
	///     Retrieves the maps payload.
	/// </summary>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>A read-only list of maps.</returns>
	/// <exception cref="HttpRequestException">Thrown when the HTTP call fails.</exception>
	/// <exception cref="JsonException">Thrown when the JSON payload is invalid.</exception>
	public async Task<IReadOnlyList<MapModel>> GetMapsAsync(CancellationToken cancellationToken = default)
	{
		var model = await HttpClient.GetFromJsonAsync<IReadOnlyList<MapModel>>(
			Constants.GuildWars.MapsUrl,
			JsonRepository.DefaultJsonOptions,
			cancellationToken);

		return model ?? throw new JsonException("Guild Wars 2 maps payload was empty.");
	}
}