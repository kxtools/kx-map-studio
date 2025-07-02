using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using KXMapStudio.App.Actions;
using KXMapStudio.App.Services;
using KXMapStudio.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace KXMapStudio.App.State;

public partial class PackStateService : ObservableObject, IPackStateService
{
    public event Action<Marker>? MarkerAdded;
    public event Action<Marker>? MarkerDeleted;

    private readonly PackService _packService;
    private readonly PackWriterService _packWriterService;
    private readonly MumbleService _mumbleService;
    private readonly IDialogService _dialogService;
    private readonly HistoryService _historyService;
    private readonly ILogger<PackStateService> _logger;

    private LoadedMarkerPack? _workspacePack;

    [ObservableProperty]
    private string? _workspacePath;

    [ObservableProperty]
    private bool _isWorkspaceLoaded;

    [ObservableProperty]
    private ObservableCollection<string> _workspaceFiles = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnsavedChanges))]
    private string? _activeDocumentPath;

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

    public bool HasUnsavedChanges =>
        (ActiveDocumentPath != null && ActiveDocumentPath.StartsWith("Untitled")) ||
        (_workspacePack?.HasUnsavedChangesFor(ActiveDocumentPath) ?? false);

    #region Constructor and Initialization

    public PackStateService(
        PackService packService,
        PackWriterService packWriterService,
        MumbleService mumbleService,
        IDialogService dialogService,
        HistoryService historyService,
        ILogger<PackStateService> logger)
    {
        _packService = packService;
        _packWriterService = packWriterService;
        _mumbleService = mumbleService;
        _dialogService = dialogService;
        _historyService = historyService;
        _logger = logger;

        MarkerAdded += OnMarkerAdded;
        MarkerDeleted += OnMarkerDeleted;
    }

    #endregion

    #region Public Methods

    public void RaiseMarkerAdded(Marker marker) => MarkerAdded?.Invoke(marker);
    public void RaiseMarkerDeleted(Marker marker) => MarkerDeleted?.Invoke(marker);

    public async Task OpenWorkspaceAsync(string path)
    {
        IsLoading = true;
        CloseWorkspaceInternal();
        try
        {
            var result = await _packService.LoadPackAsync(path);
            if (result.LoadedPack == null || !result.LoadedPack.XmlDocuments.Any())
            {
                _logger.LogWarning("PackService returned no XML documents for path: {Path}", path);
                _dialogService.ShowError("No XML Files Found", "The selected folder or pack does not contain any valid .xml marker files.");
                SetWorkspaceState(null, null);
                return;
            }

            SetWorkspaceState(path, result.LoadedPack);
            WorkspaceFiles = new ObservableCollection<string>(_workspacePack!.XmlDocuments.Keys.OrderBy(k => k));
            ActiveDocumentPath = WorkspaceFiles.FirstOrDefault();

            if (result.HasErrors)
            {
                ShowLoadPackErrors(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A catastrophic error occurred while loading from {Path}", path);
            _dialogService.ShowError("An Unexpected Error Occurred", $"A critical error occurred while trying to open the workspace.\n\nDetails: {ex.Message}");
            SetWorkspaceState(null, null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void CloseWorkspace()
    {
        CloseWorkspaceInternal();
        SetWorkspaceState(null, null);
    }

    public async Task SaveActiveDocumentAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        if (ActiveDocumentPath.StartsWith("Untitled"))
        {
            await SaveActiveDocumentAsAsync();
            return;
        }

        if (!HasUnsavedChanges)
        {
            return;
        }

        await WriteActiveDocumentToPath(WorkspacePath!, ActiveDocumentPath);
    }

    public async Task SaveActiveDocumentAsAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save Marker File As",
            Filter = "XML Marker File (*.xml)|*.xml",
            FileName = ActiveDocumentPath.StartsWith("Untitled") ? "MyRoute.xml" : Path.GetFileName(ActiveDocumentPath)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var newFilePath = dialog.FileName;
        var newFileName = Path.GetFileName(newFilePath);

        XDocument currentDoc = _workspacePack.XmlDocuments[ActiveDocumentPath];
        List<Marker> currentMarkers = new(ActiveDocumentMarkers);

        await WriteActiveDocumentToPath(newFilePath, ActiveDocumentPath, isSaveAs: true);

        _workspacePack.XmlDocuments.Remove(ActiveDocumentPath);
        _workspacePack.MarkersByFile.Remove(ActiveDocumentPath);
        _workspacePack.OriginalRawContent.Remove(ActiveDocumentPath);

        _workspacePack.XmlDocuments[newFileName] = currentDoc;
        _workspacePack.MarkersByFile[newFileName] = currentMarkers;
        foreach (var marker in currentMarkers)
        {
            marker.SourceFile = newFileName;
        }

        _workspacePack.FilePath = newFilePath;
        SetWorkspaceState(newFilePath, _workspacePack);

        WorkspaceFiles.Clear();
        WorkspaceFiles.Add(newFileName);
        ActiveDocumentPath = newFileName;
    }

    public Task NewFileAsync()
    {
        CloseWorkspaceInternal();

        _logger.LogInformation("Creating a new untitled file.");
        var newDoc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(TacoXmlConstants.OverlayDataElement,
                new XElement(TacoXmlConstants.PoisElement)
            )
        );
        var untitledName = "Untitled-1.xml";

        var newLoadedPack = new LoadedMarkerPack
        {
            FilePath = string.Empty,
            RootCategory = new Category { InternalName = "root", DisplayName = untitledName },
            OriginalRawContent = new Dictionary<string, byte[]>(),
            XmlDocuments = new Dictionary<string, XDocument> { { untitledName, newDoc } },
            MarkersByFile = new Dictionary<string, List<Marker>> { { untitledName, new List<Marker>() } }
        };
        SetWorkspaceState(null, newLoadedPack);

        WorkspaceFiles = new ObservableCollection<string> { untitledName };
        ActiveDocumentPath = untitledName;
        return Task.CompletedTask;
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

        var action = new AddMarkerAction(this, _workspacePack, newMarker);
        action.Execute();
        _historyService.Record(action);
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    public void DeleteSelectedMarkers()
    {
        if (SelectedMarkers.Count == 0 || ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        var action = new DeleteMarkersAction(this, _workspacePack, SelectedMarkers.ToList());
        action.Execute();
        _historyService.Record(action);
        SelectedMarkers.Clear();
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    #endregion

    #region Observable/Property Handlers

    partial void OnActiveDocumentPathChanged(string? value)
    {
        _logger.LogInformation("Activating document: {DocumentPath}", value);
        LoadActiveDocumentIntoView();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        _historyService.Clear();
    }

    #endregion

    #region Private Helpers

    private void LoadActiveDocumentIntoView()
    {
        SelectedMarkers.Clear();
        SelectedCategory = null;
        ActiveRootCategory = null;
        ActiveDocumentMarkers.Clear();

        if (ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        if (_workspacePack.MarkersByFile.TryGetValue(ActiveDocumentPath, out var markersForThisDoc))
        {
            foreach (var marker in markersForThisDoc)
            {
                ActiveDocumentMarkers.Add(marker);
            }
        }

        var documentRoot = new Category { DisplayName = Path.GetFileName(ActiveDocumentPath) };
        foreach (var marker in ActiveDocumentMarkers)
        {
            var destinationCategory = FindOrCreateDisplayCategory(documentRoot, marker.Type);
            if (!destinationCategory.Markers.Contains(marker))
            {
                destinationCategory.Markers.Add(marker);
            }
        }
        ActiveRootCategory = documentRoot;
    }

    private void ShowLoadPackErrors(PackLoadResult result)
    {
        var errorReport = new StringBuilder();
        errorReport.AppendLine("The pack loaded, but some files could not be read. This is usually due to data corruption in the pack file itself.\n");
        errorReport.AppendLine("Failed files:");
        foreach (var error in result.Errors)
        {
            errorReport.AppendLine($" - {error.FileName}: {error.ErrorMessage}");
            _logger.LogWarning("Load error in {FileName}: {ErrorMessage}", error.FileName, error.ErrorMessage);
        }
        _dialogService.ShowError("Pack Loaded with Errors", errorReport.ToString());
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
    }

    /// <summary>
    /// Writes the active document's content to a specified path and synchronizes the in-memory state.
    /// </summary>
    private async Task WriteActiveDocumentToPath(string saveDiskPath, string sourceKeyForWorkspace, bool isSaveAs = false)
    {
        if (_workspacePack == null)
        {
            return;
        }

        if (!_workspacePack.XmlDocuments.TryGetValue(sourceKeyForWorkspace, out var docToSave))
        {
            _logger.LogError("Attempted to save '{sourceKeyForWorkspace}' but its XDocument was not found.", sourceKeyForWorkspace);
            _dialogService.ShowError("Save Error", "Could not find the active document's XML data to save.");
            return;
        }

        _logger.LogInformation("Saving document {docKey} to disk path {path}", sourceKeyForWorkspace, saveDiskPath);
        try
        {
            _packWriterService.RewritePoisSection(docToSave, ActiveDocumentMarkers);

            var tempPath = Path.GetTempFileName();
            await using (var writer = File.CreateText(tempPath))
            {
                await docToSave.SaveAsync(writer, SaveOptions.None, CancellationToken.None);
            }

            File.Move(tempPath, saveDiskPath, true);

            using (var ms = new MemoryStream())
            {
                await docToSave.SaveAsync(ms, SaveOptions.DisableFormatting, CancellationToken.None);
                _workspacePack.OriginalRawContent[sourceKeyForWorkspace] = ms.ToArray();
            }

            _workspacePack.MarkersByFile[sourceKeyForWorkspace] = new List<Marker>(ActiveDocumentMarkers);

            _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
            _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
            foreach (var marker in ActiveDocumentMarkers)
            {
                marker.IsDirty = false;
            }

            OnPropertyChanged(nameof(HasUnsavedChanges));
            _historyService.Clear();

            _logger.LogInformation("Document {docKey} saved successfully to {path}", sourceKeyForWorkspace, saveDiskPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document {DocumentPath} to {Path}", sourceKeyForWorkspace, saveDiskPath);
            _dialogService.ShowError("Save Failed", $"An error occurred while saving the file:\n\n{ex.Message}");
        }
    }

    private Category FindOrCreateDisplayCategory(Category root, string fullNamespace)
    {
        if (string.IsNullOrEmpty(fullNamespace))
        {
            return root;
        }

        var pathParts = fullNamespace.Split('.');
        var current = root;
        foreach (var part in pathParts)
        {
            var next = current.SubCategories.FirstOrDefault(c => c.InternalName.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (next == null)
            {
                next = new Category { InternalName = part, DisplayName = part, Parent = current };
                current.SubCategories.Add(next);
            }
            current = next;
        }
        return current;
    }

    #endregion

    #region Event Handlers

    private void OnMarkerAdded(Marker marker)
    {
        if (marker.SourceFile.Equals(ActiveDocumentPath, StringComparison.OrdinalIgnoreCase) && !ActiveDocumentMarkers.Contains(marker))
        {
            ActiveDocumentMarkers.Add(marker);
        }
    }

    private void OnMarkerDeleted(Marker marker)
    {
        if (ActiveDocumentMarkers.Contains(marker))
        {
            ActiveDocumentMarkers.Remove(marker);
        }
    }

    #endregion
}
