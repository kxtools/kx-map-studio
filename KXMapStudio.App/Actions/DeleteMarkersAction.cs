using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public class DeleteMarkersAction : IAction
    {
        private readonly IPackStateService _packState;
        private readonly LoadedMarkerPack _workspacePack;
        private readonly List<(Marker Marker, int OriginalIndex)> _markersWithIndex;

        public ActionType Type => ActionType.DeleteMarkers;

        public DeleteMarkersAction(IPackStateService packState, LoadedMarkerPack workspacePack, IEnumerable<Marker> markersToDelete)
        {
            _packState = packState;
            _workspacePack = workspacePack;
            _markersWithIndex = new List<(Marker, int)>();

            // The ActiveDocumentPath should never be null when this is called, but a guard is safe.
            if (packState.ActiveDocumentPath == null) return;

            // Get the list of markers for the current file, which is the ultimate source of truth.
            var fileMarkers = workspacePack.MarkersByFile[packState.ActiveDocumentPath];
            foreach (var marker in markersToDelete)
            {
                // Find the marker in the source of truth to get its index.
                int index = fileMarkers.IndexOf(marker);
                if (index != -1)
                {
                    _markersWithIndex.Add((marker, index));
                }
            }
        }

        public void Execute()
        {
            if (_packState.ActiveDocumentPath == null) return;
            var fileMarkers = _workspacePack.MarkersByFile[_packState.ActiveDocumentPath];

            // Go in reverse order to not mess up indices when removing from the list.
            foreach (var (marker, _) in _markersWithIndex.OrderByDescending(m => m.OriginalIndex))
            {
                // 1. Modify the source of truth.
                fileMarkers.Remove(marker);

                // 2. Perform cleanup on the category tree.
                var parentCategory = FindParentCategory(marker);
                parentCategory?.Markers.Remove(marker);

                // 3. Update the "dirty" state.
                if (_workspacePack.AddedMarkers.Contains(marker))
                {
                    _workspacePack.AddedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.DeletedMarkers.Add(marker);
                }

                // 4. Announce the change to the rest of the application.
                if (_packState is PackStateService service)
                {
                    service.RaiseMarkerDeleted(marker);
                }
            }
        }

        public void Undo()
        {
            if (_packState.ActiveDocumentPath == null) return;
            var fileMarkers = _workspacePack.MarkersByFile[_packState.ActiveDocumentPath];

            // Go in forward order to restore original indices correctly.
            foreach (var (marker, originalIndex) in _markersWithIndex.OrderBy(m => m.OriginalIndex))
            {
                // 1. Restore to the source of truth at the correct index.
                fileMarkers.Insert(originalIndex, marker);

                // 2. Restore to the category tree for consistency.
                var parentCategory = FindParentCategory(marker);
                parentCategory?.Markers.Add(marker); // Note: This doesn't preserve order within the category, which is a minor, acceptable trade-off.

                // 3. Update the "dirty" state.
                if (_workspacePack.DeletedMarkers.Contains(marker))
                {
                    _workspacePack.DeletedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.AddedMarkers.Add(marker);
                }

                // 4. Announce the change.
                if (_packState is PackStateService service)
                {
                    service.RaiseMarkerAdded(marker);
                }
            }
        }

        // Helper method to find the parent category for a given marker.
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
