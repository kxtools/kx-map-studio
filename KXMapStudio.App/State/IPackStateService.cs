using System.Collections.ObjectModel;
using System.ComponentModel;

using KXMapStudio.Core;

namespace KXMapStudio.App.State;

public interface IPackStateService : INotifyPropertyChanged
{
    LoadedMarkerPack? WorkspacePack { get; }
    string? WorkspacePath { get; }
    ObservableCollection<string> WorkspaceFiles { get; }
    bool IsWorkspaceLoaded { get; }
    bool IsWorkspaceArchive { get; }

    string? ActiveDocumentPath { get; set; }
    Category? ActiveRootCategory { get; }

    ObservableCollection<Marker> ActiveDocumentMarkers { get; }

    Category? SelectedCategory { get; set; }
    ObservableCollection<Marker> SelectedMarkers { get; }
    bool IsLoading { get; }
    bool HasUnsavedChanges { get; }
    bool IsActiveDocumentDirty { get; }

    Task<bool> CheckAndPromptToSaveChanges();
    Task OpenWorkspaceAsync(string path);
    void CloseWorkspace();
    Task SaveActiveDocumentAsync();
    Task NewFileAsync();
    Task SaveActiveDocumentAsAsync();

    void InsertMarker(Marker newMarker, int insertionIndex);
    void AddMarkerFromGame();

    void DeleteMarkers(List<Marker> markersToDelete);
}
