using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public class DeleteMarkersAction : IAction
    {
        private readonly IPackStateService _packState;
        private readonly LoadedMarkerPack _workspacePack;
        private readonly List<(Marker Marker, Category ParentCategory)> _markersWithParents;

        public ActionType Type => ActionType.DeleteMarkers;

        public DeleteMarkersAction(IPackStateService packState, LoadedMarkerPack workspacePack, IEnumerable<Marker> markersToDelete)
        {
            _packState = packState;
            _workspacePack = workspacePack;
            _markersWithParents = new List<(Marker, Category)>();

            foreach (var marker in markersToDelete)
            {
                var parent = _workspacePack.RootCategory
                    .GetAllCategoriesRecursively()
                    .FirstOrDefault(c => c.Markers.Contains(marker));

                if (parent != null)
                {
                    _markersWithParents.Add((marker, parent));
                }
            }
        }

        public void Execute()
        {
            foreach (var (marker, parentCategory) in _markersWithParents)
            {
                parentCategory.Markers.Remove(marker);

                if (_workspacePack.AddedMarkers.Contains(marker))
                {
                    _workspacePack.AddedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.DeletedMarkers.Add(marker);
                }

                if (_packState is PackStateService service)
                {
                    service.RaiseMarkerDeleted(marker);
                }
            }
        }

        public void Undo()
        {
            foreach (var (marker, parentCategory) in _markersWithParents)
            {
                parentCategory.Markers.Add(marker);

                if (_workspacePack.DeletedMarkers.Contains(marker))
                {
                    _workspacePack.DeletedMarkers.Remove(marker);
                }
                else
                {
                    _workspacePack.AddedMarkers.Add(marker);
                }

                if (_packState is PackStateService service)
                {
                    service.RaiseMarkerAdded(marker);
                }
            }
        }
    }
}
