namespace KXMapStudio.Libs.Extensions;

/// <summary>
///     Provides extension methods for registering KXMapStudio.Libs dependencies into the DI container.
/// </summary>
/// <remarks>
///     This extension centralizes all presentation-layer service and ViewModel registrations,
///     ensuring consistent lifetime management and discoverability.
/// </remarks>
public static class ServiceCollectionExtension
{
	/// <summary>
	///     Registers all KXMapStudio.Libs dependencies (services and ViewModels) into the provided
	///     <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <see cref="IServiceCollection" /> instance for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <see langword="null" />.</exception>
	extension(IServiceCollection services)
	{
		public IServiceCollection AddLibsDependencies()
		{
			ArgumentNullException.ThrowIfNull(services);

			return services
				.AddServices()
				.AddViewModels();
		}

		/// <summary>
		///     Registers all presentation-layer services (UI orchestration, state management, dialogs, hotkeys).
		/// </summary>
		/// <returns>The configured <see cref="IServiceCollection" />.</returns>
		private IServiceCollection AddServices()
		{
			return services
				.AddSingleton(typeof(IStateManagementService<>), typeof(StateManagementService<>))
				.AddSingleton<IStateEqualityComparer<IReadOnlyList<GridEditorRowViewModel>>,
					GridEditorRowEqualityComparer>()
				.AddSingleton<IDispatcherHelper, DispatcherHelper>()
				.AddSingleton<IWorkshopExplorerService, WorkshopExplorerService>()
				.AddSingleton<ISaveFileDialogService, SaveFileDialogService>()
				.AddSingleton<IGridEditorDocumentService, GridEditorDocumentService>()
				.AddSingleton<IGridRowManipulationService, GridRowManipulationService>()
				.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>()
				.AddSingleton<ISnackbarMessageQueue>(_ => new SnackbarMessageQueue(TimeSpan.FromSeconds(3)))
				.AddSingleton<INotificationService, NotificationService>()
				.AddSingleton<IUpdateCheckerService, UpdateCheckerService>()
				.AddSingleton<IOpenDocumentTracker, OpenDocumentTracker>();
		}

		/// <summary>
		///     Registers all ViewModels as singletons (shared application state).
		/// </summary>
		/// <remarks>
		///     ViewModels are registered as singletons to maintain shared state across the application lifetime.
		///     If per-window state is required, convert to scoped registration and create a scope per window.
		/// </remarks>
		/// <returns>The configured <see cref="IServiceCollection" />.</returns>
		private IServiceCollection AddViewModels()
		{
			return services
				.AddSingleton<IWorkshopExplorerViewModel, WorkshopExplorerViewModel>()
				.AddSingleton<IGridEditorViewModel, GridEditorViewModel>()
				.AddSingleton<ILeftPanelViewModel, LeftSidePanelViewModel>()
				.AddSingleton<IStatusBarViewModel, StatusBarViewModel>()
				.AddSingleton<IWorkspaceWindowViewModel, WorkspaceWindowViewModel>();
		}
	}
}