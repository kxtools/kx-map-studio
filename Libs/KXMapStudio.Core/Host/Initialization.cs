namespace KXMapStudio.Core.Host;

/// <summary>
///     Hosted startup task that ensures the application data folder exists.
/// </summary>
/// <param name="Logger">The logger instance.</param>
public sealed record Initialization(ILogger<Initialization> Logger) : IHostedService
{
	/// <inheritdoc />
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var appLocation = AppContext.BaseDirectory;
		var dataFolderPath = Path.Combine(appLocation, Constants.Settings.DataFolder);

		if (!Directory.Exists(dataFolderPath))
			try
			{
				Directory.CreateDirectory(dataFolderPath);
				Logger.LogInformation("Data folder created at: {DataFolderPath}", dataFolderPath);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Failed to create Data folder at: {DataFolderPath}", dataFolderPath);
				throw;
			}
		else
			Logger.LogInformation("Data folder already exists at: {DataFolderPath}", dataFolderPath);

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}