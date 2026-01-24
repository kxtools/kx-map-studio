namespace KXMapStudio.Core.Extensions;

public static class ServiceCollectionExtensions
{
	/// <param name="services">The service collection to configure.</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		///     Adds all KXMapStudio.Core dependencies (hosting, HTTP, services, and repositories) to the provided service
		///     collection.
		/// </summary>
		/// <returns>The same <see cref="IServiceCollection" /> instance so calls can be chained.</returns>
		public IServiceCollection AddCoreDependencies()
		{
			ArgumentNullException.ThrowIfNull(services);

			return services
				.AddHostedService<Initialization>()
				.AddHttpClient()
				.AddCoreHttpClients()
				.AddCoreServices()
				.AddCoreRepositories();
		}

		private IServiceCollection AddCoreHttpClients()
		{
			// Typed clients give better discoverability and make configuration centralized.
			services.AddHttpClient<IGuildWarsHttpClient, GuildWarsHttpClient>();
			services.AddHttpClient<IGithubHttpClient, GithubHttpClient>(client =>
			{
				// GitHub API requires User-Agent header and prefers specific Accept header
				client.DefaultRequestHeaders.Add("User-Agent", "KXMapStudio");
				client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			});
			return services;
		}

		private IServiceCollection AddCoreServices()
		{
			return services
				.AddSingleton<IGw2Client, Gw2Client>()
				.AddSingleton<IMumbleService, MumbleService>()
				.AddSingleton<IJsonService, JsonService>()
				.AddSingleton<IGw2DataCacheService, Gw2DataCacheService>()
				.AddSingleton<IMumbleMarkerService, MumbleMarkerService>()
				.AddSingleton<IFileValidationService, FileValidationService>();
		}

		private IServiceCollection AddCoreRepositories()
		{
			return services
				.AddSingleton<IJsonRepository, JsonRepository>()
				.AddSingleton<IFileStorageRepository, FileStorageRepository>();
		}
	}
}