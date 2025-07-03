using System.Collections.ObjectModel;
using System.Windows;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using KXMapStudio.App.Actions;
using KXMapStudio.App.Services;
using KXMapStudio.Core;

using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.State;

public partial class PackStateService : ObservableObject, IPackStateService
{
    private readonly MumbleService _mumbleService;
    private readonly HistoryService _historyService;
    private readonly ILogger<PackStateService> _logger;
    private readonly WorkspaceManager _workspaceManager;
    private int _newFileCounter = 0;

    private LoadedMarkerPack? _workspacePack;

    public LoadedMarkerPack? WorkspacePack => _workspacePack;

    [ObservableProperty]
    private string? _workspacePath;

    [ObservableProperty]
    private bool _isWorkspaceLoaded;

    public bool IsWorkspaceArchive => _workspacePack?.IsArchive ?? false;

    [ObservableProperty]
    private ObservableCollection<string> _workspaceFiles = new();

    private string? _activeDocumentPath;
    public string? ActiveDocumentPath
    {
        get => _activeDocumentPath;
        set => SetAndLoadDocument(value);
    }

    [ObservableProperty]
    private Category? _activeRootCategory;

    [ObservableProperty]
    private ObservableCollection<Marker> _activeDocumentMarkers = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<Marker> _selectedMarkers = new();

    [ObservableProperty]
    private bool _isLoading;

    public bool HasUnsavedChanges => _workspacePack?.GetUnsavedDocumentPaths().Any() ?? false;

    public PackStateService(
        MumbleService mumbleService,
        HistoryService historyService,
        ILogger<PackStateService> logger,
        WorkspaceManager workspaceManager)
    {
        _mumbleService = mumbleService;
        _historyService = historyService;
        _logger = logger;
        _workspaceManager = workspaceManager;
    }

    #region Marker Orchestration

    public void DeleteMarkers(List<Marker> markersToDelete)
    {
        if (markersToDelete.Count == 0 || ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        var action = new DeleteMarkersAction(_workspacePack, ActiveDocumentPath, markersToDelete);

        if (action.Execute())
        {
            _historyService.Record(action);
            LoadActiveDocumentIntoView();
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public void InsertMarker(Marker newMarker, int insertionIndex)
    {
        if (_workspacePack == null)
        {
            return;
        }

        var action = new AddMarkerAction(_workspacePack, newMarker, insertionIndex);
        if (action.Execute())
        {
            _historyService.Record(action);
            LoadActiveDocumentIntoView();
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public void AddMarkerFromGame()
    {
        if (ActiveDocumentPath == null || _workspacePack == null || !_mumbleService.IsAvailable)
        {
            return;
        }

        string markerType = SelectedCategory?.FullName ?? string.Empty;
        var newMarker = new Marker
        {
            Guid = Guid.NewGuid(),
            MapId = (int)_mumbleService.CurrentMapId,
            X = _mumbleService.PlayerPosition.X,
            Y = _mumbleService.PlayerPosition.Y,
            Z = _mumbleService.PlayerPosition.Z,
            Type = markerType,
            SourceFile = ActiveDocumentPath,
        };
        newMarker.EnableChangeTracking();

        InsertMarker(newMarker, -1);
    }

    #endregion

    #region Workspace Management

    public async Task<bool> CheckAndPromptToSaveChanges()
    {
        if (_workspacePack == null)
        {
            return true;
        }

        var unsavedPaths = _workspacePack.GetUnsavedDocumentPaths().ToList();
        if (!unsavedPaths.Any())
        {
            return true;
        }

        foreach (var path in unsavedPaths)
        {
            SetAndLoadDocument(path);

            string message;
            if (IsWorkspaceArchive)
            {
                message = $"You have unsaved changes in '{path}' (from an archive). Would you like to save a copy?";
            }
            else
            {
                message = $"You have unsaved changes in '{path}'. Would you like to save them?";
            }

            var result = MessageBox.Show(message, "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    bool saveSuccess;
                    if (IsWorkspaceArchive)
                    {
                        await SaveActiveDocumentAsAsync();
                        saveSuccess = !(_workspacePack?.HasUnsavedChangesFor(path) ?? true);
                    }
                    else
                    {
                        await SaveActiveDocumentAsync();
                        saveSuccess = !(_workspacePack?.HasUnsavedChangesFor(path) ?? true);
                    }

                    if (!saveSuccess)
                    {
                        return false;
                    }
                    break;
                case MessageBoxResult.No:
                    if (_workspacePack != null)
                    {
                        _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(path, StringComparison.OrdinalIgnoreCase));
                        _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(path, StringComparison.OrdinalIgnoreCase));
                        if (_workspacePack.MarkersByFile.TryGetValue(path, out var markers))
                        {
                            foreach (var marker in markers)
                            {
                                marker.IsDirty = false;
                            }
                        }
                    }
                    OnPropertyChanged(nameof(HasUnsavedChanges));
                    continue;
                case MessageBoxResult.Cancel:
                default:
                    return false;
            }
        }

        return true;
    }

    public async Task OpenWorkspaceAsync(string path)
    {
        if (!await CheckAndPromptToSaveChanges())
        {
            return;
        }

        IsLoading = true;
        CloseWorkspaceInternal();

        var (pack, workspacePath) = await _workspaceManager.OpenWorkspaceAsync(path);

        if (pack != null)
        {
            SetWorkspaceState(workspacePath, pack);
            WorkspaceFiles = new ObservableCollection<string>(_workspacePack!.XmlDocuments.Keys.OrderBy(k => k));
            ActiveDocumentPath = WorkspaceFiles.FirstOrDefault();
        }
        else
        {
            SetWorkspaceState(null, null);
        }

        IsLoading = false;
    }

    public async void CloseWorkspace()
    {
        if (!await CheckAndPromptToSaveChanges())
        {
            return;
        }

        CloseWorkspaceInternal();
        SetWorkspaceState(null, null);
    }

    public async Task SaveActiveDocumentAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null || WorkspacePath == null)
        {
            return;
        }

        await _workspaceManager.SaveActiveDocumentAsync(_workspacePack, ActiveDocumentPath, WorkspacePath);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        _historyService.Clear();
    }

    public async Task SaveActiveDocumentAsAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        await _workspaceManager.SaveActiveDocumentAsAsync(_workspacePack, ActiveDocumentPath);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        _historyService.Clear();
    }

    public async Task NewFileAsync()
    {
        if (!await CheckAndPromptToSaveChanges())
        {
            return;
        }

        CloseWorkspaceInternal();
        _newFileCounter++;

        _logger.LogInformation("Creating a new untitled file.");
        var untitledName = $"Untitled-{_newFileCounter}.xml";

        var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(TacoXmlConstants.OverlayDataElement, new XElement(TacoXmlConstants.PoisElement)));

        var newLoadedPack = new LoadedMarkerPack
        {
            FilePath = string.Empty,
            IsArchive = false,
            RootCategory = new Category { InternalName = "root", DisplayName = untitledName },
            OriginalRawContent = new Dictionary<string, byte[]>(),
            XmlDocuments = new Dictionary<string, XDocument> { { untitledName, newDoc } },
            MarkersByFile = new Dictionary<string, List<Marker>> { { untitledName, new List<Marker>() } }
        };
        SetWorkspaceState(null, newLoadedPack);

        WorkspaceFiles = new ObservableCollection<string> { untitledName };
        ActiveDocumentPath = untitledName;
    }

    #endregion

    #region Private Helpers

    private void SetAndLoadDocument(string? newPath)
    {
        if (SetProperty(ref _activeDocumentPath, newPath, nameof(ActiveDocumentPath)))
        {
            _logger.LogInformation("Activating document: {DocumentPath}", newPath);
            LoadActiveDocumentIntoView();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            _historyService.Clear();
        }
    }

    private void LoadActiveDocumentIntoView()
    {
        var selectedCategory = this.SelectedCategory;
        ActiveDocumentMarkers.Clear();

        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            ActiveRootCategory = null;
            return;
        }

        ActiveRootCategory = _workspacePack.RootCategory;

        if (_workspacePack.MarkersByFile.TryGetValue(ActiveDocumentPath, out var markersForThisDoc))
        {
            foreach (var marker in markersForThisDoc)
            {
                ActiveDocumentMarkers.Add(marker);
            }
        }

        this.SelectedCategory = selectedCategory;
    }

    private void CloseWorkspaceInternal()
    {
        _workspacePack = null;
        WorkspacePath = null;
        ActiveDocumentPath = null;
        WorkspaceFiles.Clear();
        ActiveDocumentMarkers.Clear();
        SelectedMarkers.Clear();
        SelectedCategory = null;
        _historyService.Clear();
    }

    private void SetWorkspaceState(string? workspacePath, LoadedMarkerPack? pack)
    {
        _workspacePack = pack;
        IsWorkspaceLoaded = pack != null;
        WorkspacePath = workspacePath;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsWorkspaceArchive));
    }

    #endregion
}
