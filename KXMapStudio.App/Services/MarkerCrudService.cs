using KXMapStudio.App.Actions;
using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

public class MarkerCrudService : IMarkerCrudService
{
    private readonly MumbleService _mumbleService;
    private readonly HistoryService _historyService;

    public MarkerCrudService(MumbleService mumbleService, HistoryService historyService)
    {
        _mumbleService = mumbleService;
        _historyService = historyService;
    }

    public void DeleteMarkers(List<Marker> markersToDelete, string activeDocumentPath, LoadedMarkerPack workspacePack, IPackStateService packState)
    {
        if (markersToDelete.Count == 0 || string.IsNullOrEmpty(activeDocumentPath) || workspacePack == null)
        {
            return;
        }

        var action = new DeleteMarkersAction(packState.ActiveDocumentMarkers, markersToDelete, workspacePack);

        _historyService.Do(action);
    }

    public void InsertMarker(Marker newMarker, int insertionIndex, LoadedMarkerPack workspacePack, IPackStateService packState)
    {
        if (workspacePack == null)
        {
            return;
        }

        var action = new AddMarkerAction(packState.ActiveDocumentMarkers, newMarker, insertionIndex, workspacePack);
        _historyService.Do(action);
    }

    public void AddMarkerFromGame(string activeDocumentPath, LoadedMarkerPack workspacePack, string? selectedCategoryFullName, IPackStateService packState)
    {
        if (string.IsNullOrEmpty(activeDocumentPath) || workspacePack == null || !_mumbleService.IsAvailable)
        {
            return;
        }

        string markerType = selectedCategoryFullName ?? string.Empty;
        var newMarker = new Marker
        {
            Guid = Guid.NewGuid(),
            MapId = (int)_mumbleService.CurrentMapId,
            X = _mumbleService.PlayerPosition.X,
            Y = _mumbleService.PlayerPosition.Y,
            Z = _mumbleService.PlayerPosition.Z,
            Type = markerType,
            SourceFile = activeDocumentPath,
        };
        newMarker.EnableChangeTracking();

        InsertMarker(newMarker, -1, workspacePack, packState);
    }
}
