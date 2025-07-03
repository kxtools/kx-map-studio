using KXMapStudio.Core;
using KXMapStudio.App.State;

namespace KXMapStudio.App.Services;

public interface IMarkerCrudService
{
    void DeleteMarkers(List<Marker> markersToDelete, string activeDocumentPath, LoadedMarkerPack workspacePack, IPackStateService packState);
    void InsertMarker(Marker newMarker, int insertionIndex, LoadedMarkerPack workspacePack, IPackStateService packState);
    void AddMarkerFromGame(string activeDocumentPath, LoadedMarkerPack workspacePack, string? selectedCategoryFullName, IPackStateService packState);
}
