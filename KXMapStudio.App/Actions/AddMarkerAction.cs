using System.Collections.ObjectModel;
using KXMapStudio.App.Services;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions;

public class AddMarkerAction : IAction
{
    private readonly ObservableCollection<Marker> _activeDocumentMarkers;
    private readonly Marker _addedMarker;
    private readonly int _insertionIndex;

    public ActionType Type => ActionType.AddMarker;

    public AddMarkerAction(ObservableCollection<Marker> activeDocumentMarkers, Marker markerToAdd, int insertionIndex = -1)
    {
        _activeDocumentMarkers = activeDocumentMarkers;
        _addedMarker = markerToAdd;
        _insertionIndex = insertionIndex;
    }

    public bool Execute()
    {
        if (_insertionIndex >= 0 && _insertionIndex <= _activeDocumentMarkers.Count)
        {
            _activeDocumentMarkers.Insert(_insertionIndex, _addedMarker);
        }
        else
        {
            _activeDocumentMarkers.Add(_addedMarker);
        }
        return true;
    }

    public bool Undo()
    {
        _activeDocumentMarkers.Remove(_addedMarker);
        return true;
    }
}
