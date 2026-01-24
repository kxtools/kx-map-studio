namespace KXMapStudio.Core.Types.Structs;

public readonly ref partial struct Constants
{
	/// <summary>
	///     Contains constants related to the Guild Wars 2 external API.
	/// </summary>
	public readonly ref struct GuildWars
	{
		/// <summary>
		///     Gets the GW2 API endpoint that returns continent floor data used by the application.
		/// </summary>
		public const string ContinentsUrl = "https://api.guildwars2.com/v2/continents/1/floors/1";

		/// <summary>
		///     Gets the GW2 API endpoint that returns the full maps payload.
		/// </summary>
		public const string MapsUrl = "https://api.guildwars2.com/v2/maps?ids=all";
	}
}