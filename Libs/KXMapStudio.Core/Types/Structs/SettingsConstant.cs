namespace KXMapStudio.Core.Types.Structs;

public readonly ref partial struct Constants
{
	/// <summary>
	///     Contains constants related to KXMapStudio runtime settings and well-known paths.
	/// </summary>
	public readonly ref struct Settings
	{
		/// <summary>
		///     Gets the name of the runtime data folder created next to the application binaries.
		/// </summary>
		public const string DataFolder = "Data";

		/// <summary>
		///     Gets the default file name for the cached continents payload.
		/// </summary>
		public const string ContinentsPath = "continents.json";

		/// <summary>
		///     Gets the default file name for the cached maps payload.
		/// </summary>
		public const string MapsPath = "maps.json";

		/// <summary>
		///     Gets the KXTools website URL.
		/// </summary>
		public const string KxToolsWebsiteUrl = "https://kxtools.xyz";

		/// <summary>
		///     Gets the KXTools slogan string.
		/// </summary>
		public const string KxToolsSlogan = "Check out our other tools and projects at kxtools.xyz.";

		/// <summary>
		///     Gets the Discord invite URL.
		/// </summary>
		public const string DiscordInviteUrl = "https://discord.gg/z92rnB4kHm";

		/// <summary>
		///     Gets the GitHub repository URL for KX Map Studio.
		/// </summary>
		public const string GitHubRepoUrl = "https://github.com/kxtools/kx-map-studio";

		/// <summary>
		///     Gets the GitHub API URL used to query the latest release.
		/// </summary>
		public const string GitHubApiUrl = "https://api.github.com/repos/kxtools/kx-map-studio/releases/latest";

		/// <summary>
		///     Gets the default buffer size (in bytes) used for stream operations.
		/// </summary>
		public const int DefaultBufferSize = 4096;
	}
}