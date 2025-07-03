using KXMapStudio.Core;

namespace KXMapStudio.App.State;

public partial class PackStateService
{
    private void CloseWorkspaceInternal()
    {
        _workspacePack = null;
        WorkspacePath = null;
        ActiveDocumentPath = null;
        WorkspaceFiles.Clear();
        ActiveDocumentMarkers.Clear();
        SelectedMarkers.Clear();
        SelectedCategory = null;
    }

    private void SetWorkspaceState(string? workspacePath, LoadedMarkerPack? pack)
    {
        _workspacePack = pack;
        IsWorkspaceLoaded = pack != null;
        WorkspacePath = workspacePath;
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsWorkspaceArchive));
    }
}
