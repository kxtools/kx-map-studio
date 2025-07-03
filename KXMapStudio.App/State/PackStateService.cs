using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
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
    private readonly PackService _packService;
    private readonly PackWriterService _packWriterService;
    private readonly MumbleService _mumbleService;
    private readonly IDialogService _dialogService;
    private readonly HistoryService _historyService;
    private readonly ILogger<PackStateService> _logger;
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
    }

    #region Marker Orchestration

    public void DeleteMarkers(List<Marker> markersToDelete)
    {
        if (markersToDelete.Count == 0 || ActiveDocumentPath == null || _workspacePack == null)
        {
            return;
        }

        // We are now using the list passed directly from the UI, bypassing the potentially desynced collection.
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
        if (_workspacePack == null) return;

        var action = new AddMarkerAction(_workspacePack, newMarker, insertionIndex);
        if (action.Execute())
        {
            _historyService.Record(action);
            // After changing the model, just reload the view from the model.
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

        // Use the append functionality of InsertMarker
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
                        saveSuccess = await SaveAsAndContinueAsync();
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

        if (WorkspacePath == null)
        {
            _logger.LogError("Attempted to save an existing document, but WorkspacePath is null.");
            _dialogService.ShowError("Save Error", "Cannot save the file because the original path is unknown. Please use 'Save As...'.");
            return;
        }

        string fullSavePath = Directory.Exists(WorkspacePath)
            ? Path.Combine(WorkspacePath, ActiveDocumentPath)
            : WorkspacePath;

        await WriteActiveDocumentToPath(fullSavePath, ActiveDocumentPath);
    }

    public async Task SaveActiveDocumentAsAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Save Marker File As",
            Filter = "XML Marker File (*.xml)|*.xml",
            FileName = ActiveDocumentPath.StartsWith("Untitled") ? "MyRoute.xml" : Path.GetFileName(ActiveDocumentPath)
        };

        if (dialog.ShowDialog() != true) return;

        var newFilePath = dialog.FileName;

        if (Path.GetExtension(newFilePath).ToLowerInvariant() is ".taco" or ".zip")
        {
            _dialogService.ShowError("Invalid Save Location", "Saving directly into a .taco or .zip archive is not supported. Please save as a standard .xml file.");
            return;
        }

        var newFileName = Path.GetFileName(newFilePath);
        var markersToSave = _workspacePack.MarkersByFile[ActiveDocumentPath];
        _workspacePack.UnmanagedPois.TryGetValue(ActiveDocumentPath, out var unmanagedElements);

        var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(TacoXmlConstants.OverlayDataElement));
        _packWriterService.RewritePoisSection(newDoc, markersToSave, unmanagedElements ?? Enumerable.Empty<XElement>());

        if (!await WriteDocumentToDiskAsync(newDoc, newFilePath)) return;

        var newLoadedPack = await _packService.LoadPackAsync(newFilePath);

        CloseWorkspaceInternal();
        SetWorkspaceState(newFilePath, newLoadedPack.LoadedPack);
        WorkspaceFiles = new ObservableCollection<string>(_workspacePack!.XmlDocuments.Keys.OrderBy(k => k));
        ActiveDocumentPath = WorkspaceFiles.FirstOrDefault();

        _logger.LogInformation("File saved as '{newFilePath}' and workspace context has been reset to a single-file mode.", newFilePath);
    }

    public async Task NewFileAsync()
    {
        if (!await CheckAndPromptToSaveChanges()) return;

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
        // We need to save and restore the selection across a full reload.
        var selectedCategory = this.SelectedCategory;
        // NOTE: We don't save/restore marker selection as it's cleared on delete/add.

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

        // Restore the category selection.
        this.SelectedCategory = selectedCategory;
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
        OnPropertyChanged(nameof(IsWorkspaceArchive));
    }

    private async Task WriteActiveDocumentToPath(string saveDiskPath, string sourceKeyForWorkspace)
    {
        if (_workspacePack == null || !_workspacePack.XmlDocuments.TryGetValue(sourceKeyForWorkspace, out var docToSave))
        {
            _logger.LogError("Attempted to save '{sourceKey}' but its XDocument was not found.", sourceKeyForWorkspace);
            _dialogService.ShowError("Save Error", "Could not find the active document's XML data to save.");
            return;
        }

        _workspacePack.UnmanagedPois.TryGetValue(sourceKeyForWorkspace, out var unmanagedElements);
        var markersInOrder = _workspacePack.MarkersByFile[sourceKeyForWorkspace];
        _packWriterService.RewritePoisSection(docToSave, markersInOrder, unmanagedElements ?? Enumerable.Empty<XElement>());

        if (!await WriteDocumentToDiskAsync(docToSave, saveDiskPath)) return;

        _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
        _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
        foreach (var marker in markersInOrder)
        {
            marker.IsDirty = false;
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        _historyService.Clear();

        _logger.LogInformation("Document {docKey} saved successfully to {path}", sourceKeyForWorkspace, saveDiskPath);
    }

    private async Task<bool> SaveAsAndContinueAsync()
    {
        if (ActiveDocumentPath == null || _workspacePack == null) return false;

        var dialog = new SaveFileDialog
        {
            Title = "Save Copy As",
            Filter = "XML Marker File (*.xml)|*.xml",
            FileName = Path.GetFileName(ActiveDocumentPath)
        };

        if (dialog.ShowDialog() != true) return false;

        var newFilePath = dialog.FileName;

        if (Path.GetExtension(newFilePath).ToLowerInvariant() is ".taco" or ".zip")
        {
            _dialogService.ShowError("Invalid Save Location", "Saving directly into a .taco or .zip archive is not supported. Please save as a standard .xml file.");
            return false;
        }

        _workspacePack.UnmanagedPois.TryGetValue(ActiveDocumentPath, out var unmanagedElements);
        var markersToSave = _workspacePack.MarkersByFile[ActiveDocumentPath];
        var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(TacoXmlConstants.OverlayDataElement));
        _packWriterService.RewritePoisSection(newDoc, markersToSave, unmanagedElements ?? Enumerable.Empty<XElement>());

        if (await WriteDocumentToDiskAsync(newDoc, newFilePath))
        {
            _workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(ActiveDocumentPath, StringComparison.OrdinalIgnoreCase));
            _workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(ActiveDocumentPath, StringComparison.OrdinalIgnoreCase));
            foreach (var marker in markersToSave)
            {
                marker.IsDirty = false;
            }
            OnPropertyChanged(nameof(HasUnsavedChanges));
            _historyService.Clear();
            return true;
        }
        return false;
    }

    private async Task<bool> WriteDocumentToDiskAsync(XDocument doc, string path)
    {
        try
        {
            var tempPath = Path.GetTempFileName();
            await using (var writer = File.CreateText(tempPath))
            {
                await doc.SaveAsync(writer, SaveOptions.None, CancellationToken.None);
            }
            File.Move(tempPath, path, true);
            _logger.LogInformation("Successfully wrote document to disk at {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write document to disk at {Path}", path);
            _dialogService.ShowError("Save Failed", $"An error occurred while writing the file to disk:\n\n{ex.Message}");
            return false;
        }
    }

    #endregion
}
