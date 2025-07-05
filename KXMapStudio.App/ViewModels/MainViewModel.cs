using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KXMapStudio.App.Actions;
using KXMapStudio.App.Models;
using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.App.Utilities;
using KXMapStudio.App.ViewModels.PropertyEditor;
using KXMapStudio.Core;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace KXMapStudio.App.ViewModels;

// Using [ObservableObject] for property change notifications
public partial class MainViewModel : ObservableObject
{
    #region Public Commands for XAML Binding
    // These are now all public get-only properties, initialized in the constructor.
    public IAsyncRelayCommand NewFileCommand { get; }
    public IAsyncRelayCommand OpenFileCommand { get; }
    public IAsyncRelayCommand OpenFolderCommand { get; }
    public IAsyncRelayCommand CloseWorkspaceCommand { get; }
    public IAsyncRelayCommand SaveDocumentCommand { get; }
    public IAsyncRelayCommand SaveAsCommand { get; }
    public IRelayCommand AddMarkerFromGameCommand { get; }
    public IRelayCommand UndoCommand { get; }
    public IRelayCommand RedoCommand { get; }
    public IRelayCommand UndoLastAddedMarkerCommand { get; }
    public IRelayCommand<Marker?> InsertNewMarkerCommand { get; }
    public IRelayCommand<object> SelectCategoryCommand { get; }
    public IRelayCommand AcknowledgeUpdateCommand { get; }
    public IRelayCommand<string?> OpenLinkCommand { get; }
    public IRelayCommand OpenKxToolsWebsiteCommand { get; }
    public IRelayCommand OpenDiscordLinkCommand { get; }
    public IRelayCommand OpenGitHubLinkCommand { get; }

    // Commands that were causing binding errors are now correctly defined.
    public IRelayCommand DeleteMarkersCommand { get; }
    public IRelayCommand MoveMarkersUpCommand { get; }
    public IRelayCommand MoveMarkersDownCommand { get; }
    public IRelayCommand CopySelectedMarkerGuidCommand { get; }
    #endregion

    #region Services and State
    public IPackStateService PackState { get; }
    public MumbleService MumbleService { get; }
    public PropertyEditorViewModel PropertyEditorViewModel { get; }
    public GlobalHotkeyService GlobalHotkeys => _globalHotkeyService;
    public string AppVersion { get; }

    private readonly IFeedbackService _feedbackService;
    private readonly HistoryService _historyService;
    private readonly GlobalHotkeyService _globalHotkeyService;
    private readonly UpdateService _updateService;
    private readonly MapDataService _mapDataService;
    #endregion

    #region Observable Properties
    [ObservableProperty] private ObservableCollection<Marker> _markersInView = new();
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private GitHubRelease? _latestRelease;
    [ObservableProperty] private string _liveMapName = "Loading...";
    #endregion

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

    public MainViewModel(
        IPackStateService packStateService, MumbleService mumbleService, PropertyEditorViewModel propertyEditorViewModel,
        IFeedbackService feedbackService, HistoryService historyService, GlobalHotkeyService globalHotkeyService,
        UpdateService updateService, MapDataService mapDataService)
    {
        // Assign services
        PackState = packStateService;
        MumbleService = mumbleService;
        PropertyEditorViewModel = propertyEditorViewModel;
        _feedbackService = feedbackService;
        _historyService = historyService;
        _globalHotkeyService = globalHotkeyService;
        _updateService = updateService;
        _mapDataService = mapDataService;

        AppVersion = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown"}";

        // Initialize commands
        NewFileCommand = new AsyncRelayCommand(PackState.NewFileAsync);
        OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        CloseWorkspaceCommand = new AsyncRelayCommand(CloseWorkspaceAsync, () => PackState.IsWorkspaceLoaded);
        SaveDocumentCommand = new AsyncRelayCommand(SaveDocumentAsync, () => PackState.HasUnsavedChanges && !PackState.IsWorkspaceArchive);
        SaveAsCommand = new AsyncRelayCommand(PackState.SaveActiveDocumentAsAsync, () => PackState.IsWorkspaceLoaded);
        UndoCommand = new RelayCommand(_historyService.Undo, () => _historyService.CanUndo);
        RedoCommand = new RelayCommand(_historyService.Redo, () => _historyService.CanRedo);
        UndoLastAddedMarkerCommand = new RelayCommand(TryUndoLastAddMarker, () => _historyService.CanUndo);
        AddMarkerFromGameCommand = new RelayCommand(AddMarkerFromGame, () => PackState.ActiveDocumentPath != null && MumbleService.IsAvailable);
        DeleteMarkersCommand = new RelayCommand(DeleteSelectedMarkers, () => PackState.SelectedMarkers.Any());
        MoveMarkersUpCommand = new RelayCommand(MoveSelectedMarkersUp, CanMoveSelectedMarkersUp);
        MoveMarkersDownCommand = new RelayCommand(MoveSelectedMarkersDown, CanMoveSelectedMarkersDown);
        CopySelectedMarkerGuidCommand = new RelayCommand(CopySelectedMarkerGuid, () => PackState.SelectedMarkers.Count == 1);
        InsertNewMarkerCommand = new RelayCommand<Marker?>(InsertNewMarker, _ => PackState.ActiveDocumentPath != null);
        SelectCategoryCommand = new RelayCommand<object>(HandleSelectCategory);
        AcknowledgeUpdateCommand = new RelayCommand(() => { if (LatestRelease != null)
            {
                OpenLink(LatestRelease.HtmlUrl);
            }

            IsUpdateAvailable = false; });
        OpenLinkCommand = new RelayCommand<string?>(OpenLink);
        OpenKxToolsWebsiteCommand = new RelayCommand(() => OpenLinkCommand.Execute(Constants.KxToolsWebsiteUrl));
        OpenDiscordLinkCommand = new RelayCommand(() => OpenLinkCommand.Execute(Constants.DiscordInviteUrl));
        OpenGitHubLinkCommand = new RelayCommand(() => OpenLinkCommand.Execute(Constants.GitHubRepoUrl));

        // Wire up events and startup tasks
        WireEvents();
        SetupHotkeys();
        _ = CheckForUpdatesOnStartup();
    }

    #region Command Implementations
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog { Title = "Open Marker File", Filter = "Supported Files (*.taco, *.zip, *.xml)|*.taco;*.zip;*.xml|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            await PackState.OpenWorkspaceAsync(dialog.FileName);
        }
    }

    private async Task OpenFolderAsync()
    {
        var dialog = new CommonOpenFileDialog { Title = "Select Workspace Folder", IsFolderPicker = true };
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            await PackState.OpenWorkspaceAsync(dialog.FileName);
        }
    }

    private async Task CloseWorkspaceAsync()
    {
        if (await PackState.CheckAndPromptToSaveChanges())
        {
            PackState.CloseWorkspace();
        }
    }

    private Task SaveDocumentAsync() => PackState.SaveActiveDocumentAsync();

    private void AddMarkerFromGame()
    {
        PackState.AddMarkerFromGame();
        if (PackState.ActiveDocumentMarkers.Any())
        {
            var newMarker = PackState.ActiveDocumentMarkers.Last();
            PackState.SelectedMarkers.Clear();
            PackState.SelectedMarkers.Add(newMarker);
        }
    }

    private void TryUndoLastAddMarker()
    {
        if (_historyService.CanUndo && _historyService.PeekLastActionType() == ActionType.AddMarker)
        {
            _historyService.Undo();
            _feedbackService.ShowMessage("Undid last marker addition via hotkey.");
        }
        else
        {
            _feedbackService.ShowMessage("Cannot undo: last action was not a marker addition.", actionContent: "OK");
        }
    }

    public void DeleteSelectedMarkers()
    {
        var markersToDelete = PackState.SelectedMarkers.ToList();
        PackState.DeleteMarkers(markersToDelete);
    }

    public void MoveSelectedMarkersUp()
    {
        var action = new ReorderMarkersAction(PackState.ActiveDocumentMarkers, PackState.SelectedMarkers, ReorderDirection.Up);
        _historyService.Do(action);
    }
    private bool CanMoveSelectedMarkersUp()
    {
        if (!PackState.SelectedMarkers.Any())
        {
            return false;
        }

        int minIndex = PackState.SelectedMarkers.Min(m => PackState.ActiveDocumentMarkers.IndexOf(m));
        return minIndex > 0;
    }

    public void MoveSelectedMarkersDown()
    {
        var action = new ReorderMarkersAction(PackState.ActiveDocumentMarkers, PackState.SelectedMarkers, ReorderDirection.Down);
        _historyService.Do(action);
    }
    private bool CanMoveSelectedMarkersDown()
    {
        if (!PackState.SelectedMarkers.Any())
        {
            return false;
        }

        int maxIndex = PackState.SelectedMarkers.Max(m => PackState.ActiveDocumentMarkers.IndexOf(m));
        return maxIndex < PackState.ActiveDocumentMarkers.Count - 1;
    }

    private void CopySelectedMarkerGuid()
    {
        if (PackState.SelectedMarkers.Count == 1)
        {
            var guid = PackState.SelectedMarkers.First().GuidFormatted;
            Clipboard.SetText(guid);
            _feedbackService.ShowMessage($"Copied GUID: {guid}", "DISMISS");
        }
    }

    public void InsertNewMarker(Marker? rightClickedMarker)
    {
        if (PackState.WorkspacePack == null || PackState.ActiveDocumentPath == null)
        {
            return;
        }

        int insertionIndex = rightClickedMarker != null
            ? PackState.ActiveDocumentMarkers.IndexOf(rightClickedMarker) + 1
            : PackState.ActiveDocumentMarkers.Count;
        var newMarker = new Marker
        {
            Guid = Guid.NewGuid(),
            MapId = rightClickedMarker?.MapId ?? (int)MumbleService.CurrentMapId,
            X = rightClickedMarker?.X ?? MumbleService.PlayerPosition.X,
            Y = rightClickedMarker?.Y ?? MumbleService.PlayerPosition.Y,
            Z = rightClickedMarker?.Z ?? MumbleService.PlayerPosition.Z,
            Type = rightClickedMarker?.Type ?? PackState.SelectedCategory?.FullName ?? string.Empty,
            SourceFile = PackState.ActiveDocumentPath,
        };
        newMarker.EnableChangeTracking();
        PackState.InsertMarker(newMarker, insertionIndex);
        PackState.SelectedMarkers.Clear();
        PackState.SelectedMarkers.Add(newMarker);
    }

    private void OpenLink(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _feedbackService.ShowMessage($"Could not open link: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Event Handlers and Helpers
    private void WireEvents()
    {
        PackState.ActiveDocumentMarkers.CollectionChanged += OnActiveDocumentMarkersChanged;
        PackState.PropertyChanged += OnPackStateChanged;
        PackState.SelectedMarkers.CollectionChanged += OnSelectedMarkersChanged;
        _historyService.PropertyChanged += OnHistoryChanged;
        MumbleService.PropertyChanged += OnMumbleServiceChanged;
        _mapDataService.MapDataRefreshed += OnMapDataRefreshed;
    }

    private void SetupHotkeys()
    {
        _globalHotkeyService.AddMarkerHotkeyPressed += (s, e) => AddMarkerFromGameCommand.Execute(null);
        _globalHotkeyService.UndoLastAddHotkeyPressed += (s, e) => UndoLastAddedMarkerCommand.Execute(null);
    }

    private void OnPackStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IPackStateService.IsWorkspaceLoaded):
                CloseWorkspaceCommand.NotifyCanExecuteChanged();
                SaveAsCommand.NotifyCanExecuteChanged();
                AddMarkerFromGameCommand.NotifyCanExecuteChanged();
                DeleteMarkersCommand.NotifyCanExecuteChanged();
                InsertNewMarkerCommand.NotifyCanExecuteChanged();
                goto case nameof(IPackStateService.HasUnsavedChanges);
            case nameof(IPackStateService.HasUnsavedChanges):
            case nameof(IPackStateService.IsWorkspaceArchive):
                OnPropertyChanged(nameof(Title));
                SaveDocumentCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.ActiveDocumentPath):
                OnPropertyChanged(nameof(Title));
                UpdateMarkersInView();
                AddMarkerFromGameCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.SelectedCategory):
                UpdateMarkersInView();
                break;
        }
    }

    private void OnSelectedMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CopySelectedMarkerGuidCommand.NotifyCanExecuteChanged();
        MoveMarkersUpCommand.NotifyCanExecuteChanged();
        MoveMarkersDownCommand.NotifyCanExecuteChanged();
        DeleteMarkersCommand.NotifyCanExecuteChanged();
    }

    private void OnHistoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistoryService.CanUndo))
        {
            UndoCommand.NotifyCanExecuteChanged();
            UndoLastAddedMarkerCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(HistoryService.CanRedo))
        {
            RedoCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnMumbleServiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MumbleService.IsAvailable))
        {
            AddMarkerFromGameCommand.NotifyCanExecuteChanged();
        }
        if (e.PropertyName == nameof(MumbleService.CurrentMapId) || e.PropertyName == nameof(MumbleService.IsAvailable))
        {
            LiveMapName = MumbleService.IsAvailable
                ? _mapDataService.GetMapData((int)MumbleService.CurrentMapId)?.Name ?? "N/A"
                : "N/A";
        }
    }

    private void OnMapDataRefreshed()
    {
        OnMumbleServiceChanged(this, new(nameof(MumbleService.CurrentMapId)));
        var markers = MarkersInView.ToList();
        MarkersInView.Clear();
        foreach (var marker in markers)
        {
            MarkersInView.Add(marker);
        }
    }

    private void OnActiveDocumentMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateMarkersInView();

    public Task<bool> RequestChangeDocumentAsync() => PackState.CheckAndPromptToSaveChanges();

    private void HandleSelectCategory(object? eventArgs)
    {
        if (eventArgs is RoutedPropertyChangedEventArgs<object> args)
        {
            PackState.SelectedCategory = args.NewValue as Category;
        }
    }

    private void UpdateMarkersInView()
    {
        var currentSelection = PackState.SelectedMarkers.ToList();
        MarkersInView.Clear();

        var markersToDisplay = PackState.SelectedCategory == null
            ? PackState.ActiveDocumentMarkers
            : PackState.ActiveDocumentMarkers.Where(m =>
                m.Type != null && m.Type.StartsWith(PackState.SelectedCategory.FullName, StringComparison.OrdinalIgnoreCase));

        foreach (var marker in markersToDisplay)
        {
            MarkersInView.Add(marker);
        }

        var markersToRemoveFromSelection = currentSelection.Where(m => !MarkersInView.Contains(m)).ToList();
        foreach (var markerToRemove in markersToRemoveFromSelection)
        {
            PackState.SelectedMarkers.Remove(markerToRemove);
        }
    }

    private async Task CheckForUpdatesOnStartup()
    {
        var (isNewVersionAvailable, latestRelease) = await _updateService.CheckForUpdatesAsync();
        if (isNewVersionAvailable && latestRelease != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LatestRelease = latestRelease;
                IsUpdateAvailable = true;
            });
        }
    }
    #endregion
}
