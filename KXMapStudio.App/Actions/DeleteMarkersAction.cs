using KXMapStudio.App.Services;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public class DeleteMarkersAction : IAction
    {
        private readonly LoadedMarkerPack _workspacePack;
        private readonly List<(Marker Marker, int OriginalIndex)> _markersWithIndex;
        private readonly string _activeDocumentPath;

        public ActionType Type => ActionType.DeleteMarkers;

        public DeleteMarkersAction(LoadedMarkerPack workspacePack, string activeDocumentPath, IEnumerable<Marker> markersToDelete)
        {
            _workspacePack = workspacePack;
            _activeDocumentPath = activeDocumentPath;
            _markersWithIndex = new List<(Marker, int)>();

            var fileMarkers = workspacePack.MarkersByFile[_activeDocumentPath];
            foreach (var marker in markersToDelete)
            {
                int index = fileMarkers.IndexOf(marker);
                if (index != -1)
                {
                    _markersWithIndex.Add((marker, index));
                }
            }
        }

        public bool Execute()
        {
            var fileMarkers = _workspacePack.MarkersByFile[_activeDocumentPath];

            foreach (var (marker, _) in _markersWithIndex.OrderByDescending(m => m.OriginalIndex))
            {
                fileMarkers.Remove(marker);
                FindParentCategory(marker)?.Markers.Remove(marker);

                if (_workspacePack.AddedMarkers.Contains(marker))
                {
                    _workspacePack.AddedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.DeletedMarkers.Add(marker);
                }
            }
            return true;
        }

        public bool Undo()
        {
            var fileMarkers = _workspacePack.MarkersByFile[_activeDocumentPath];

            foreach (var (marker, originalIndex) in _markersWithIndex.OrderBy(m => m.OriginalIndex))
            {
                fileMarkers.Insert(originalIndex, marker);
                FindParentCategory(marker)?.Markers.Add(marker);

                if (_workspacePack.DeletedMarkers.Contains(marker))
                {
                    _workspacePack.DeletedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.AddedMarkers.Add(marker);
                }
            }
            return true;
        }

        private Category? FindParentCategory(Marker marker)
        {
            if (string.IsNullOrEmpty(marker.Type))
            {
                return _workspacePack.RootCategory;
            }
            return _workspacePack.RootCategory
                       .GetAllCategoriesRecursively()
                       .FirstOrDefault(c => c.FullName.Equals(marker.Type, StringComparison.OrdinalIgnoreCase));
        }
    }
}
