using KXMapStudio.App.Services;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions;

public class AddMarkerAction : IAction
{
    private readonly LoadedMarkerPack _workspacePack;
    private readonly Marker _addedMarker;
    private readonly int _insertionIndex;
    private readonly Category _parentCategory;

    public ActionType Type => ActionType.AddMarker;

    public AddMarkerAction(LoadedMarkerPack workspacePack, Marker markerToAdd, int insertionIndex = -1)
    {
        _workspacePack = workspacePack;
        _addedMarker = markerToAdd;
        _insertionIndex = insertionIndex;
        _parentCategory = FindParentCategory();
    }

    public bool Execute()
    {
        _parentCategory.Markers.Add(_addedMarker);

        if (_workspacePack.MarkersByFile.TryGetValue(_addedMarker.SourceFile, out var markersForFile))
        {
            if (_insertionIndex >= 0 && _insertionIndex < markersForFile.Count)
            {
                markersForFile.Insert(_insertionIndex, _addedMarker);
            }
            else
            {
                markersForFile.Add(_addedMarker);
            }
        }

        _workspacePack.AddedMarkers.Add(_addedMarker);
        return true;
    }

    public bool Undo()
    {
        _parentCategory.Markers.Remove(_addedMarker);

        if (_workspacePack.MarkersByFile.TryGetValue(_addedMarker.SourceFile, out var markersForFile))
        {
            markersForFile.Remove(_addedMarker);
        }

        _workspacePack.AddedMarkers.Remove(_addedMarker);
        return true;
    }

    private Category FindParentCategory()
    {
        if (string.IsNullOrEmpty(_addedMarker.Type))
        {
            return _workspacePack.RootCategory;
        }

        return _workspacePack.RootCategory
                   .GetAllCategoriesRecursively()
                   .FirstOrDefault(c => c.FullName.Equals(_addedMarker.Type, StringComparison.OrdinalIgnoreCase))
               ?? _workspacePack.RootCategory;
    }
}
