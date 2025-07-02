using System.Windows;

using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.App.ViewModels;
using KXMapStudio.App.ViewModels.PropertyEditor;
using KXMapStudio.App.Views;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace KXMapStudio.App
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File("Logs/KXMapStudio-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            AppHost = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MainView>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<PropertyEditorViewModel>();
                    services.AddSingleton<PackService>();
                    services.AddSingleton<PackWriterService>();
                    services.AddSingleton<MumbleService>();
                    services.AddSingleton<IDialogService, WpfDialogService>();

                    services.AddSingleton(new SnackbarMessageQueue());
                    services.AddSingleton<ISnackbarMessageQueue>(sp => sp.GetRequiredService<SnackbarMessageQueue>());
                    services.AddSingleton<IFeedbackService, SnackbarFeedbackService>();

                    services.AddSingleton<HistoryService>();
                    services.AddSingleton<GlobalHotkeyService>();

                    services.AddSingleton<IPackStateService, PackStateService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            Log.Information("Application starting up.");
            await AppHost!.StartAsync();

            var mumbleService = AppHost.Services.GetRequiredService<MumbleService>();
            mumbleService.Start();

            var startupForm = AppHost.Services.GetRequiredService<MainView>();
            startupForm.Show();

            var hotkeyService = AppHost.Services.GetRequiredService<GlobalHotkeyService>();
            hotkeyService.RegisterHotkeys();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down.");
            await Log.CloseAndFlushAsync();

            if (AppHost is not null)
            {
                var hotkeyService = AppHost.Services.GetRequiredService<GlobalHotkeyService>();
                hotkeyService.Dispose();

                var mumbleService = AppHost.Services.GetRequiredService<MumbleService>();
                mumbleService.Dispose();

                await AppHost.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
