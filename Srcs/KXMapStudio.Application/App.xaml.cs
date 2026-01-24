namespace KXMapStudio.Application;

public sealed partial class App
{
	public App()
	{
		AppHost = Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration(configuration => { configuration.AddJsonFile("appsettings.json"); })
			.ConfigureServices(services =>
			{
				services
					.AddCoreDependencies()
					.AddLibsDependencies()
					.AddSingleton<WorkspaceWindow>();
			})
			.Build();
	}

	private static IHost AppHost { get; set; } = null!;

	protected override void OnStartup(StartupEventArgs e)
	{
		ForceCulture("en-US");
		base.OnStartup(e);
		AppHost.Start();

		// Start MumbleLink polling so the StatusBar stays up-to-date.
		AppHost.Services.GetRequiredService<IMumbleService>().Start();

		// Check for application updates asynchronously and show notification
		_ = CheckForUpdatesAndNotifyAsync();

		var mainWindow = AppHost.Services.GetRequiredService<WorkspaceWindow>();
		mainWindow.Show();
	}

	private static async Task CheckForUpdatesAndNotifyAsync()
	{
		try
		{
			var updateChecker = AppHost.Services.GetRequiredService<IUpdateCheckerService>();
			var notificationService = AppHost.Services.GetRequiredService<INotificationService>();

			// Check for updates (non-blocking)
			await updateChecker.CheckForUpdatesAsync();

			// Show notification based on update status
			if (updateChecker.IsUpdateAvailable)
				notificationService.ShowInfo($"A new version is available: {updateChecker.LatestVersionTag}");
			else
				notificationService.ShowSuccess("Application is up to date!");
		}
		catch (Exception ex)
		{
			// Log but don't disrupt startup if version check fails
			// Using a basic Console.WriteLine since logger may not be available here
			Console.WriteLine($"Failed to check for updates: {ex.Message}");
		}
	}

	private static void ForceCulture(string cultureName)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);

		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		Thread.CurrentThread.CurrentCulture = culture;
		Thread.CurrentThread.CurrentUICulture = culture;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		// Stop background polling before shutting down hosting.
		try
		{
			AppHost.Services.GetRequiredService<IMumbleService>().Stop();
		}
		catch
		{
			// Intentional no-op.
		}

		AppHost.StopAsync().GetAwaiter().GetResult();
		AppHost.Dispose();

		base.OnExit(e);
	}
}