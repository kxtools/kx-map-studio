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

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private GitHubRelease? _latestRelease;

    public MainViewModel(
        IPackStateService packStateService,
        MumbleService mumbleService,
        PropertyEditorViewModel propertyEditorViewModel,
        IFeedbackService feedbackService,
        HistoryService historyService,
        GlobalHotkeyService globalHotkeyService,
        UpdateService updateService)
    {
        PackState = packStateService;
        MumbleService = mumbleService;
        PropertyEditorViewModel = propertyEditorViewModel;
        _feedbackService = feedbackService;
        _historyService = historyService;
        _globalHotkeyService = globalHotkeyService;
        _updateService = updateService;

        AppVersion = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown"}";

        SetupCommands();
        WireEvents();
        SetupHotkeys();

        _ = CheckForUpdatesOnStartup();
    }

    private void SetupCommands()
    {
        _selectCategoryCommand = new RelayCommand<object>(HandleSelectCategory);
        _newFileCommand = new AsyncRelayCommand(PackState.NewFileAsync);
        _saveAsCommand = new AsyncRelayCommand(PackState.SaveActiveDocumentAsAsync, () => PackState.IsWorkspaceLoaded);

        _moveSelectedMarkersUpCommand = new RelayCommand(() => MoveMarkersUp(PackState.SelectedMarkers.ToList()), () => CanMoveSelectedMarkersUp());
        _moveSelectedMarkersDownCommand = new RelayCommand(() => MoveMarkersDown(PackState.SelectedMarkers.ToList()), () => CanMoveSelectedMarkersDown());
        _copySelectedMarkerGuidCommand = new RelayCommand(() => CopySelectedMarkerGuid(PackState.SelectedMarkers.ToList()), () => PackState.SelectedMarkers.Count == 1);

        _insertNewMarkerCommand = new RelayCommand(
            () => InsertNewMarker(PackState.SelectedMarkers.ToList()),
            () => PackState.ActiveDocumentPath != null);
        _openFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        _openFileCommand = new AsyncRelayCommand(OpenFileAsync);
        _closeWorkspaceCommand = new AsyncRelayCommand(CloseWorkspaceAsync, () => PackState.IsWorkspaceLoaded);
        _saveDocumentCommand = new AsyncRelayCommand(SaveDocumentAsync, () => PackState.HasUnsavedChanges && !PackState.IsWorkspaceArchive);
        _addMarkerFromGameCommand = new RelayCommand(AddMarkerFromGame, () => PackState.ActiveDocumentPath != null && MumbleService.IsAvailable);
        _undoCommand = new RelayCommand(_historyService.Undo, () => _historyService.CanUndo);
        _redoCommand = new RelayCommand(_historyService.Redo, () => _historyService.CanRedo);

        _openLinkCommand = new RelayCommand<string>(OpenLink);
        _openKxToolsWebsiteCommand = new RelayCommand(() => _openLinkCommand.Execute(Constants.KxToolsWebsiteUrl));
        _openDiscordLinkCommand = new RelayCommand(() => _openLinkCommand.Execute(Constants.DiscordInviteUrl));
        _openGitHubLinkCommand = new RelayCommand(() => _openLinkCommand.Execute(Constants.GitHubRepoUrl));
    }

    private void WireEvents()
    {
        PackState.ActiveDocumentMarkers.CollectionChanged += OnActiveDocumentMarkersChanged;
        PackState.PropertyChanged += OnPackStateChanged;
        PackState.SelectedMarkers.CollectionChanged += OnSelectedMarkersChanged;
        _historyService.PropertyChanged += OnHistoryChanged;
        MumbleService.PropertyChanged += OnMumbleServiceChanged;
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

            var unsavedIndicator = PackState.HasUnsavedChanges ? "*" : "";
            var documentName = Path.GetFileName(PackState.ActiveDocumentPath);

            if (PackState.IsWorkspaceArchive)
            {
                var archiveName = Path.GetFileName(PackState.WorkspacePath);
                return $"{documentName}{unsavedIndicator} (in {archiveName}) - KX Map Studio";
            }

            return $"{documentName}{unsavedIndicator} - KX Map Studio";
        }
    }

    public Task<bool> RequestChangeDocumentAsync()
    {
        // Delegate the check to the state service.
        return PackState.CheckAndPromptToSaveChanges();
    }

    private void OnPackStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IPackStateService.IsWorkspaceLoaded):
            case nameof(IPackStateService.HasUnsavedChanges):
            case nameof(IPackStateService.IsWorkspaceArchive):
                OnPropertyChanged(nameof(Title));
                CloseWorkspaceCommand.NotifyCanExecuteChanged();
                SaveDocumentCommand.NotifyCanExecuteChanged();
                SaveAsCommand.NotifyCanExecuteChanged();
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
        //DeleteSelectedMarkersCommand.NotifyCanExecuteChanged();
        CopySelectedMarkerGuidCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersUpCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersDownCommand.NotifyCanExecuteChanged();
    }

    private void OnHistoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistoryService.CanUndo))
        {
            UndoCommand.NotifyCanExecuteChanged();
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
            // When Mumble's availability changes, tell the command to re-evaluate its state.
            AddMarkerFromGameCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnActiveDocumentMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UpdateMarkersInView();
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<Marker>())
            {
                MarkersInView.Remove(item);
            }
        }

        if (e.NewItems != null)
        {
            // Handle adding at a specific index if the event provides it.
            if (e.NewStartingIndex > -1)
            {
                int index = e.NewStartingIndex;
                foreach (var item in e.NewItems.OfType<Marker>())
                {
                    MarkersInView.Insert(index++, item);
                }
            }
            else
            {
                foreach (var item in e.NewItems.OfType<Marker>())
                {
                    MarkersInView.Add(item);
                }
            }
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

    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog { Title = "Open Marker File", Filter = "Supported Files (*.taco, *.zip, *.xml)|*.taco;*.zip;*.xml|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            await PackState.OpenWorkspaceAsync(dialog.FileName);
        }
    }

    private Task CloseWorkspaceAsync()
    {
        PackState.CloseWorkspace();
        return Task.CompletedTask;
    }

    private async Task SaveDocumentAsync()
    {
        if (PackState.ActiveDocumentPath == null)
        {
            return;
        }

        await PackState.SaveActiveDocumentAsync();

        if (!PackState.HasUnsavedChanges)
        {
            _feedbackService.ShowMessage($"Saved {Path.GetFileName(PackState.ActiveDocumentPath)}");
        }
    }

    private void AddMarkerFromGame()
    {
        PackState.AddMarkerFromGame();
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (PackState.ActiveDocumentMarkers.Any())
            {
                var newMarker = PackState.ActiveDocumentMarkers.Last();
                PackState.SelectedMarkers.Clear();
                PackState.SelectedMarkers.Add(newMarker);
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    public void CopySelectedMarkerGuid(List<Marker> selectedMarkers)
    {
        if (selectedMarkers.Count != 1)
        {
            // Maybe provide feedback that you can only copy one at a time.
            _feedbackService.ShowMessage("Please select a single marker to copy its GUID.", "OK");
            return;
        }

        var guid = selectedMarkers.First().GuidFormatted;
        Clipboard.SetText(guid);
        _feedbackService.ShowMessage($"Copied GUID: {guid}", "DISMISS");
    }

    private void HandleSelectCategory(object? eventArgs)
    {
        if (eventArgs is RoutedPropertyChangedEventArgs<object> args)
        {
            PackState.SelectedCategory = args.NewValue as Category;
        }
    }

    public void MoveMarkersUp(List<Marker> markersToMove)
    {
        // The action now receives the explicit list, not the potentially stale collection.
        var action = new ReorderMarkersAction(PackState.ActiveDocumentMarkers, markersToMove, ReorderDirection.Up);
        action.Execute();
        _historyService.Record(action);
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

    public void MoveMarkersDown(List<Marker> markersToMove)
    {
        var action = new ReorderMarkersAction(PackState.ActiveDocumentMarkers, markersToMove, ReorderDirection.Down);
        action.Execute();
        _historyService.Record(action);
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

    [RelayCommand]
    private void AcknowledgeUpdate()
    {
        if (LatestRelease != null)
        {
            OpenLink(LatestRelease.HtmlUrl);
            IsUpdateAvailable = false;
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

    public void InsertNewMarker(List<Marker> currentSelection)
    {
        if (PackState.WorkspacePack == null || PackState.ActiveDocumentPath == null)
        {
            return;
        }

        int insertionIndex = 0;
        Marker? selectedMarker = null;

        var markersForFile = PackState.WorkspacePack.MarkersByFile[PackState.ActiveDocumentPath];

        if (markersForFile.Any() && currentSelection.Any())
        {
            // Find the topmost selected marker from the provided list.
            selectedMarker = currentSelection
                .OrderBy(m => markersForFile.IndexOf(m))
                .FirstOrDefault();

            if (selectedMarker != null)
            {
                insertionIndex = markersForFile.IndexOf(selectedMarker);
                if (insertionIndex == -1) { insertionIndex = 0; }
            }
        }

        var newMarker = new Marker
        {
            Guid = Guid.NewGuid(),
            MapId = selectedMarker?.MapId ?? (int)MumbleService.CurrentMapId,
            X = selectedMarker?.X ?? MumbleService.PlayerPosition.X,
            Y = selectedMarker?.Y ?? MumbleService.PlayerPosition.Y,
            Z = selectedMarker?.Z ?? MumbleService.PlayerPosition.Z,
            Type = selectedMarker?.Type ?? PackState.SelectedCategory?.FullName ?? string.Empty,
            SourceFile = PackState.ActiveDocumentPath,
        };
        newMarker.EnableChangeTracking();

        PackState.InsertMarker(newMarker, insertionIndex);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            PackState.SelectedMarkers.Clear();
            PackState.SelectedMarkers.Add(newMarker);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
}
