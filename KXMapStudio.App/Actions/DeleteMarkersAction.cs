using System.Collections.ObjectModel;
using KXMapStudio.App.Services;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions;

public class DeleteMarkersAction : IAction
{
    private readonly ObservableCollection<Marker> _activeDocumentMarkers;
    private readonly List<(Marker Marker, int OriginalIndex)> _markersWithIndex;

    public ActionType Type => ActionType.DeleteMarkers;

    public DeleteMarkersAction(ObservableCollection<Marker> activeDocumentMarkers, IEnumerable<Marker> markersToDelete)
    {
        _activeDocumentMarkers = activeDocumentMarkers;
        _markersWithIndex = new List<(Marker, int)>();

        foreach (var marker in markersToDelete)
        {
            int index = _activeDocumentMarkers.IndexOf(marker);
            if (index != -1)
            {
                _markersWithIndex.Add((marker, index));
            }
        }
    }

    public bool Execute()
    {
        foreach (var (marker, _) in _markersWithIndex.OrderByDescending(m => m.OriginalIndex))
        {
            _activeDocumentMarkers.Remove(marker);
        }
        return true;
    }

    public bool Undo()
    {
        foreach (var (marker, originalIndex) in _markersWithIndex.OrderBy(m => m.OriginalIndex))
        {
            _activeDocumentMarkers.Insert(originalIndex, marker);
        }
        return true;
    }
}
