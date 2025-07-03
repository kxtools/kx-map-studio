using System.Collections.ObjectModel;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using KXMapStudio.App.Services;
using KXMapStudio.App.Services.Pack;
using KXMapStudio.Core;

using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.State;

public partial class PackStateService : ObservableObject, IPackStateService
{

    private readonly ILogger<PackStateService> _logger;
    private readonly WorkspaceManager _workspaceManager;
    private readonly IMarkerCrudService _markerCrudService;


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
    public bool IsActiveDocumentDirty => _workspacePack?.HasUnsavedChangesFor(ActiveDocumentPath) ?? false;

    private readonly IFeedbackService _feedbackService;
    private readonly ISavePromptService _savePromptService;
    private readonly IDirtyStateTracker _dirtyStateTracker;

    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public PackStateService(
        IMarkerCrudService markerCrudService,
        ILogger<PackStateService> logger,
        WorkspaceManager workspaceManager,
        IFeedbackService feedbackService,
        ISavePromptService savePromptService,
        IDirtyStateTracker dirtyStateTracker,
        MarkerXmlParser markerXmlParser,
        CategoryBuilder categoryBuilder)
    {
        _markerCrudService = markerCrudService;
        _logger = logger;
        _workspaceManager = workspaceManager;
        _feedbackService = feedbackService;
        _savePromptService = savePromptService;
        _dirtyStateTracker = dirtyStateTracker;
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    #region Marker Orchestration

    public void DeleteMarkers(List<Marker> markersToDelete)
    {
        if (markersToDelete == null || !markersToDelete.Any() || ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        // Capture original indices before deletion
        var originalIndices = markersToDelete
            .Select(m => ActiveDocumentMarkers.IndexOf(m))
            .Where(idx => idx != -1) // Ensure marker is actually in the current view
            .OrderBy(idx => idx)
            .ToList();

        _markerCrudService.DeleteMarkers(markersToDelete, ActiveDocumentPath!, _workspacePack!, this);

        // Determine next selection
        if (ActiveDocumentMarkers.Any())
        {
            Marker? nextSelection = null;
            // Try to select the marker immediately after the last deleted marker
            var lastDeletedOriginalIndex = originalIndices.LastOrDefault();
            if (lastDeletedOriginalIndex != -1 && lastDeletedOriginalIndex < ActiveDocumentMarkers.Count)
            {
                nextSelection = ActiveDocumentMarkers[lastDeletedOriginalIndex];
            }
            // If no marker after, try to select the one before the first deleted marker
            else if (originalIndices.Any() && originalIndices.First() > 0)
            {
                nextSelection = ActiveDocumentMarkers[originalIndices.First() - 1];
            }
            // Fallback to first item if nothing else works
            else
            {
                nextSelection = ActiveDocumentMarkers.FirstOrDefault();
            }

            SelectedMarkers.Clear();
            if (nextSelection != null)
            {
                SelectedMarkers.Add(nextSelection);
            }
        }
        else
        {
            SelectedMarkers.Clear();
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsActiveDocumentDirty));
    }

    public void InsertMarker(Marker newMarker, int insertionIndex)
    {
        _markerCrudService.InsertMarker(newMarker, insertionIndex, _workspacePack!, this);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsActiveDocumentDirty));
    }

    public void AddMarkerFromGame()
    {
        _markerCrudService.AddMarkerFromGame(ActiveDocumentPath!, _workspacePack!, SelectedCategory?.FullName, this);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsActiveDocumentDirty));
    }

    #endregion

    #region Workspace Management

    public async Task<bool> CheckAndPromptToSaveChanges()
    {
        return await _savePromptService.PromptToSaveChanges(this);
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
            _dirtyStateTracker.StartTracking(ActiveDocumentMarkers, this);
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

        _dirtyStateTracker.StopTracking(ActiveDocumentMarkers);
        CloseWorkspaceInternal();
        SetWorkspaceState(null, null);
    }

    public async Task NewFileAsync()
    {
        if (!await CheckAndPromptToSaveChanges())
        {
            return;
        }

        _dirtyStateTracker.StopTracking(ActiveDocumentMarkers);
        CloseWorkspaceInternal();


        _logger.LogInformation("Creating a new untitled file.");
        var untitledName = $"Untitled-{DateTime.Now:yyyyMMddHHmmss}.xml";

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
        // Clear and add to existing ActiveDocumentMarkers instance
        ActiveDocumentMarkers.Clear();
        // No markers to add for a new file, so ActiveDocumentMarkers remains empty
        ActiveDocumentPath = untitledName;
        _dirtyStateTracker.StartTracking(ActiveDocumentMarkers, this);
    }

    public async Task SaveActiveDocumentAsync()
    {
        if (ActiveDocumentPath != null && ActiveDocumentPath.StartsWith("Untitled-"))
        {
            await SaveActiveDocumentAsAsync();
            return;
        }

        if (ActiveDocumentPath == null || _workspacePack == null || WorkspacePath == null)
        {
            return;
        }

        await _workspaceManager.SaveActiveDocumentAsync(_workspacePack, ActiveDocumentPath, WorkspacePath, ActiveDocumentMarkers);
        OnPropertyChanged(nameof(HasUnsavedChanges));

    }

    public async Task SaveActiveDocumentAsAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        var (success, newFilePath) = await _workspaceManager.SaveDocumentAsAsync(_workspacePack, ActiveDocumentPath, ActiveDocumentMarkers);

        if (success)
        {
            if (ActiveDocumentPath.StartsWith("Untitled"))
            {
                // If it was an untitled file, the workspace now becomes this single file.
                // We need to reload the pack to ensure all internal structures are correct.
                var newLoadedPackResult = await _workspaceManager.OpenWorkspaceAsync(newFilePath!);
                if (newLoadedPackResult.pack != null)
                {
                    SetWorkspaceState(newLoadedPackResult.path, newLoadedPackResult.pack);
                    WorkspaceFiles = new ObservableCollection<string>(_workspacePack!.XmlDocuments.Keys.OrderBy(k => k));
                    ActiveDocumentPath = WorkspaceFiles.FirstOrDefault();
                }
            }
            else
            {
                // For existing files, just clear dirty state.
                _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(ActiveDocumentPath, StringComparison.OrdinalIgnoreCase));
                _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(ActiveDocumentPath, StringComparison.OrdinalIgnoreCase));
                if (_workspacePack.MarkersByFile.TryGetValue(ActiveDocumentPath, out var markers))
                {
                    foreach (var marker in markers)
                    {
                        marker.IsDirty = false;
                    }
                }
            }

            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(IsActiveDocumentDirty));
            _feedbackService.ShowMessage("A copy was saved successfully. You are still working in the original workspace.");
        }
    }

    #endregion

    #region Private Helpers

    private void SetAndLoadDocument(string? newPath)
    {
        if (SetProperty(ref _activeDocumentPath, newPath, nameof(ActiveDocumentPath)))
        {
            _logger.LogInformation("Activating document: {DocumentPath}", newPath);
            LoadActiveDocumentIntoView();
        }
    }

    public void LoadActiveDocumentIntoView()
    {
        var selectedCategory = this.SelectedCategory;

        // Stop tracking old markers before clearing
        _dirtyStateTracker.StopTracking(ActiveDocumentMarkers);

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

        // Start tracking new markers after populating
        _dirtyStateTracker.StartTracking(ActiveDocumentMarkers, this);

        this.SelectedCategory = selectedCategory;
    }

    public void UpdateDirtyState()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsActiveDocumentDirty));
    }

    public void RevertDocumentChanges(string documentPath)
    {
        if (_workspacePack == null || !_workspacePack.OriginalRawContent.TryGetValue(documentPath, out var originalBytes))
        {
            return;
        }

        _workspacePack.MarkersByFile.Remove(documentPath);
        _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(documentPath, StringComparison.OrdinalIgnoreCase));
        _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(documentPath, StringComparison.OrdinalIgnoreCase));

        var packLoader = new PackLoader(_markerXmlParser, _categoryBuilder);
        var result = packLoader.LoadPackFromMemoryAsync(new Dictionary<string, byte[]> { { documentPath, originalBytes } }, _workspacePack.FilePath, _workspacePack.IsArchive).Result;

        if (result.LoadedPack != null && result.LoadedPack.MarkersByFile.TryGetValue(documentPath, out var revertedMarkers))
        {
            _workspacePack.MarkersByFile[documentPath] = revertedMarkers;
        }

        LoadActiveDocumentIntoView();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsActiveDocumentDirty));
    }

    #endregion
}
