using System.Collections.ObjectModel;
using System.ComponentModel;

using KXMapStudio.Core;

namespace KXMapStudio.App.State;

public interface IPackStateService : INotifyPropertyChanged
{
    event Action<Marker> MarkerAdded;
    event Action<Marker> MarkerDeleted;

    string? WorkspacePath { get; }
    ObservableCollection<string> WorkspaceFiles { get; }
    bool IsWorkspaceLoaded { get; }

    string? ActiveDocumentPath { get; set; }
    Category? ActiveRootCategory { get; }

    ObservableCollection<Marker> ActiveDocumentMarkers { get; }

    Category? SelectedCategory { get; set; }
    ObservableCollection<Marker> SelectedMarkers { get; }
    bool IsLoading { get; }
    bool HasUnsavedChanges { get; }

    Task OpenWorkspaceAsync(string path);
    void CloseWorkspace();
    Task SaveActiveDocumentAsync();
    Task NewFileAsync();
    Task SaveActiveDocumentAsAsync();

    void AddMarkerFromGame();
    void DeleteSelectedMarkers();
}
