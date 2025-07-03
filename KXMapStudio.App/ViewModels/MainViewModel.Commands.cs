using CommunityToolkit.Mvvm.Input;
using KXMapStudio.App.Actions;
using KXMapStudio.App.Utilities;
using KXMapStudio.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace KXMapStudio.App.ViewModels;

public partial class MainViewModel
{
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

    private async Task OpenFolderAsync()
    {
        var dialog = new CommonOpenFileDialog { Title = "Select Workspace Folder", IsFolderPicker = true };
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            await PackState.OpenWorkspaceAsync(dialog.FileName);
        }
        else
        {
            _feedbackService.ShowMessage("Folder selection cancelled.");
        }
    }

    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog { Title = "Open Marker File", Filter = "Supported Files (*.taco, *.zip, *.xml)|*.taco;*.zip;*.xml|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            await PackState.OpenWorkspaceAsync(dialog.FileName);
        }
        else
        {
            _feedbackService.ShowMessage("File selection cancelled.");
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
            _feedbackService.ShowMessage("Please select a single marker to copy its GUID.", "OK");
            return;
        }

        var guid = selectedMarkers.First().GuidFormatted;
        Clipboard.SetText(guid);
        _feedbackService.ShowMessage($"Copied GUID: {guid}", "DISMISS");
    }

    public void MoveMarkersUp(List<Marker> markersToMove)
    {
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

    public void InsertNewMarker(List<Marker> currentSelection)
    {
        if (PackState.WorkspacePack == null || PackState.ActiveDocumentPath == null)
        {
            return;
        }

        var markersForFile = PackState.WorkspacePack.MarkersByFile[PackState.ActiveDocumentPath];

        int insertionIndex = markersForFile.Count;
        Marker? selectedMarker = null;

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
