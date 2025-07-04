using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KXMapStudio.App.Models;
using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.App.ViewModels.PropertyEditor;
using KXMapStudio.Core;

namespace KXMapStudio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private IRelayCommand _selectCategoryCommand = null!;
    private IAsyncRelayCommand _openFolderCommand = null!;
    private IAsyncRelayCommand _openFileCommand = null!;
    private IAsyncRelayCommand _closeWorkspaceCommand = null!;
    private IAsyncRelayCommand _saveDocumentCommand = null!;
    private IRelayCommand _addMarkerFromGameCommand = null!;
    private IRelayCommand _copySelectedMarkerGuidCommand = null!;
    private IRelayCommand _undoCommand = null!;
    private IRelayCommand _redoCommand = null!;
    private IRelayCommand _moveSelectedMarkersUpCommand = null!;
    private IRelayCommand _moveSelectedMarkersDownCommand = null!;
    private IAsyncRelayCommand _newFileCommand = null!;
    private IAsyncRelayCommand _saveAsCommand = null!;
    private IRelayCommand _openKxToolsWebsiteCommand = null!;
    private IRelayCommand _openDiscordLinkCommand = null!;
    private IRelayCommand _openGitHubLinkCommand = null!;
    private IRelayCommand<string> _openLinkCommand = null!;
    private IRelayCommand _insertNewMarkerCommand = null!;

    public IRelayCommand SelectCategoryCommand => _selectCategoryCommand;
    public IAsyncRelayCommand OpenFolderCommand => _openFolderCommand;
    public IAsyncRelayCommand OpenFileCommand => _openFileCommand;
    public IAsyncRelayCommand CloseWorkspaceCommand => _closeWorkspaceCommand;
    public IAsyncRelayCommand SaveDocumentCommand => _saveDocumentCommand;
    public IRelayCommand AddMarkerFromGameCommand => _addMarkerFromGameCommand;
    public IRelayCommand CopySelectedMarkerGuidCommand => _copySelectedMarkerGuidCommand;
    public IRelayCommand UndoCommand => _undoCommand;
    public IRelayCommand RedoCommand => _redoCommand;
    public IRelayCommand MoveSelectedMarkersUpCommand => _moveSelectedMarkersUpCommand;
    public IRelayCommand MoveSelectedMarkersDownCommand => _moveSelectedMarkersDownCommand;
    public IAsyncRelayCommand NewFileCommand => _newFileCommand;
    public IAsyncRelayCommand SaveAsCommand => _saveAsCommand;
    public IRelayCommand OpenKxToolsWebsiteCommand => _openKxToolsWebsiteCommand;
    public IRelayCommand OpenDiscordLinkCommand => _openDiscordLinkCommand;
    public IRelayCommand OpenGitHubLinkCommand => _openGitHubLinkCommand;
    public IRelayCommand<string> OpenLinkCommand => _openLinkCommand;
    public IRelayCommand InsertNewMarkerCommand => _insertNewMarkerCommand;

    public IPackStateService PackState { get; }
    public MumbleService MumbleService { get; }
    public PropertyEditorViewModel PropertyEditorViewModel { get; }

    [ObservableProperty]
    private ObservableCollection<Marker> _markersInView = new();

    public GlobalHotkeyService GlobalHotkeys => _globalHotkeyService;
    public string AppVersion { get; }

    private readonly IFeedbackService _feedbackService;
    private readonly HistoryService _historyService;
    private readonly GlobalHotkeyService _globalHotkeyService;
    private readonly UpdateService _updateService;
    private readonly MapDataService _mapDataService;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private GitHubRelease? _latestRelease;

    [ObservableProperty]
    private string _liveMapName = "Loading...";

    public MainViewModel(
        IPackStateService packStateService,
        MumbleService mumbleService,
        PropertyEditorViewModel propertyEditorViewModel,
        IFeedbackService feedbackService,
        HistoryService historyService,
        GlobalHotkeyService globalHotkeyService,
        UpdateService updateService,
        MapDataService mapDataService)
    {
        PackState = packStateService;
        MumbleService = mumbleService;
        PropertyEditorViewModel = propertyEditorViewModel;
        _feedbackService = feedbackService;
        _historyService = historyService;
        _globalHotkeyService = globalHotkeyService;
        _updateService = updateService;
        _mapDataService = mapDataService;

        AppVersion = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown"}";

        SetupCommands();
        WireEvents();
        SetupHotkeys();

        _ = CheckForUpdatesOnStartup();
    }

    private void SetupHotkeys()
    {
        _globalHotkeyService.AddMarkerHotkeyPressed += (s, e) => AddMarkerFromGameCommand.Execute(null);
        _globalHotkeyService.UndoLastAddHotkeyPressed += (s, e) => TryUndoLastAddMarker();
    }

    public void DeleteMarkers(List<Marker> markersToDelete)
    {
        PackState.DeleteMarkers(markersToDelete);
    }

    public string Title
    {
        get
        {
            if (!PackState.IsWorkspaceLoaded || string.IsNullOrEmpty(PackState.ActiveDocumentPath))
            {
                return "KX Map Studio";
            }

            var unsavedIndicator = PackState.IsActiveDocumentDirty ? "*" : "";
            var documentName = Path.GetFileName(PackState.ActiveDocumentPath);

            if (PackState.IsWorkspaceArchive)
            {
                var archiveName = Path.GetFileName(PackState.WorkspacePath);
                return $"{documentName}{unsavedIndicator} (in {archiveName}) [Read-Only] - KX Map Studio";
            }

            return $"{documentName}{unsavedIndicator} - KX Map Studio";
        }
    }

    public Task<bool> RequestChangeDocumentAsync()
    {
        return PackState.CheckAndPromptToSaveChanges();
    }

    private void HandleSelectCategory(object? eventArgs)
    {
        if (eventArgs is System.Windows.RoutedPropertyChangedEventArgs<object> args)
        {
            PackState.SelectedCategory = args.NewValue as Category;
        }
    }

    private void UpdateMarkersInView()
    {
        var selectionToRestore = new List<Marker>(PackState.SelectedMarkers);
        MarkersInView.Clear();

        var selectedCategory = PackState.SelectedCategory;
        if (selectedCategory == null)
        {
            if (PackState.IsWorkspaceLoaded)
            {
                foreach (var marker in PackState.ActiveDocumentMarkers)
                {
                    MarkersInView.Add(marker);
                }
            }
        }
        else
        {
            var markersToDisplay = PackState.ActiveDocumentMarkers.Where(m =>
                m.Type != null && m.Type.StartsWith(selectedCategory.FullName, System.StringComparison.OrdinalIgnoreCase));
            foreach (var marker in markersToDisplay)
            {
                MarkersInView.Add(marker);
            }
        }

        PackState.SelectedMarkers.Clear();
        foreach (var marker in selectionToRestore)
        {
            if (MarkersInView.Contains(marker))
            {
                PackState.SelectedMarkers.Add(marker);
            }
        }
    }

    private async Task CheckForUpdatesOnStartup()
    {
        var (isNewVersionAvailable, latestRelease) = await _updateService.CheckForUpdatesAsync();

        if (isNewVersionAvailable && latestRelease != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LatestRelease = latestRelease;
                IsUpdateAvailable = true;
            });
        }
    }
}
