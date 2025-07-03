using KXMapStudio.App.Services;
using KXMapStudio.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.ObjectModel;

namespace KXMapStudio.App.State;

public class WorkspaceManager
{
    private readonly PackService _packService;
    private readonly PackWriterService _packWriterService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WorkspaceManager> _logger;

    public WorkspaceManager(
        PackService packService,
        PackWriterService packWriterService,
        IDialogService dialogService,
        ILogger<WorkspaceManager> logger)
    {
        _packService = packService;
        _packWriterService = packWriterService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public async Task<(LoadedMarkerPack? pack, string? path)> OpenWorkspaceAsync(string path)
    {
        try
        {
            var result = await _packService.LoadPackAsync(path);
            if (result.LoadedPack == null || !result.LoadedPack.XmlDocuments.Any())
            {
                _logger.LogWarning("PackService returned no XML documents for path: {Path}", path);
                _dialogService.ShowError("No XML Files Found", "The selected folder or pack does not contain any valid .xml marker files.");
                return (null, null);
            }

            if (result.HasErrors)
            {
                ShowLoadPackErrors(result);
            }

            return (result.LoadedPack, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A catastrophic error occurred while loading from {Path}", path);
            _dialogService.ShowError("An Unexpected Error Occurred", $"A critical error occurred while trying to open the workspace.\n\nDetails: {ex.Message}");
            return (null, null);
        }
    }

    public async Task SaveActiveDocumentAsync(LoadedMarkerPack workspacePack, string activeDocumentPath, string workspacePath, ObservableCollection<Marker> activeDocumentMarkers)
    {
        if (activeDocumentPath.StartsWith("Untitled"))
        {
            await SaveDocumentAsAsync(workspacePack, activeDocumentPath, activeDocumentMarkers);
            return;
        }

        if (workspacePath == null)
        {
            _logger.LogError("Attempted to save an existing document, but WorkspacePath is null.");
            _dialogService.ShowError("Save Error", "Cannot save the file because the original path is unknown. Please use 'Save As...'.");
            return;
        }

        string fullSavePath = Directory.Exists(workspacePath)
            ? Path.Combine(workspacePath, activeDocumentPath)
            : workspacePath;

        await WriteActiveDocumentToPath(workspacePack, fullSavePath, activeDocumentPath, activeDocumentMarkers);
    }

    public async Task<(bool success, string? newFilePath)> SaveDocumentAsAsync(LoadedMarkerPack workspacePack, string activeDocumentPath, ObservableCollection<Marker> activeDocumentMarkers)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Marker File As",
            Filter = "XML Marker File (*.xml)|*.xml",
            FileName = activeDocumentPath.StartsWith("Untitled") ? "MyRoute.xml" : Path.GetFileName(activeDocumentPath)
        };

        if (dialog.ShowDialog() != true)
        {
            return (false, null);
        }

        var newFilePath = dialog.FileName;

        if (Path.GetExtension(newFilePath).ToLowerInvariant() is ".taco" or ".zip")
        {
            _dialogService.ShowError("Invalid Save Location", "Saving directly into a .taco or .zip archive is not supported. Please save as a standard .xml file.");
            return (false, null);
        }

        // Use activeDocumentMarkers instead of workspacePack.MarkersByFile[activeDocumentPath]
        workspacePack.UnmanagedPois.TryGetValue(activeDocumentPath, out var unmanagedElements);

        var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(TacoXmlConstants.OverlayDataElement));
        _packWriterService.RewritePoisSection(newDoc, activeDocumentMarkers, unmanagedElements ?? Enumerable.Empty<XElement>());

        if (await WriteDocumentToDiskAsync(newDoc, newFilePath))
        {
            return (true, newFilePath);
        }
        return (false, null);
    }

    private async Task WriteActiveDocumentToPath(LoadedMarkerPack workspacePack, string saveDiskPath, string sourceKeyForWorkspace, ObservableCollection<Marker> activeDocumentMarkers)
    {
        if (!workspacePack.XmlDocuments.TryGetValue(sourceKeyForWorkspace, out var docToSave))
        {
            _logger.LogError("Attempted to save '{sourceKey}' but its XDocument was not found.", sourceKeyForWorkspace);
            _dialogService.ShowError("Save Error", "Could not find the active document's XML data to save.");
            return;
        }

        workspacePack.UnmanagedPois.TryGetValue(sourceKeyForWorkspace, out var unmanagedElements);
        // Use activeDocumentMarkers instead of workspacePack.MarkersByFile[sourceKeyForWorkspace]
        _packWriterService.RewritePoisSection(docToSave, activeDocumentMarkers, unmanagedElements ?? Enumerable.Empty<XElement>());

        if (!await WriteDocumentToDiskAsync(docToSave, saveDiskPath))
        {
            return;
        }

        workspacePack.AddedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
        workspacePack.DeletedMarkers.RemoveWhere(m => m.SourceFile.Equals(sourceKeyForWorkspace, StringComparison.OrdinalIgnoreCase));
        foreach (var marker in activeDocumentMarkers)
        {
            marker.IsDirty = false;
        }

        _logger.LogInformation("Document {docKey} saved successfully to {path}", sourceKeyForWorkspace, saveDiskPath);
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
}
