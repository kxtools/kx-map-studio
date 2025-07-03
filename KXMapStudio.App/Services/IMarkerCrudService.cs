using KXMapStudio.Core;
using System.Collections.Generic;

namespace KXMapStudio.App.Services;

public interface IMarkerCrudService
{
    void DeleteMarkers(List<Marker> markersToDelete, string activeDocumentPath, LoadedMarkerPack workspacePack);
    void InsertMarker(Marker newMarker, int insertionIndex, LoadedMarkerPack workspacePack);
    void AddMarkerFromGame(string activeDocumentPath, LoadedMarkerPack workspacePack, string? selectedCategoryFullName);
}