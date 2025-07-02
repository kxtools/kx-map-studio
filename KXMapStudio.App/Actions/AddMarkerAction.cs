using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public class AddMarkerAction : IAction
    {
        private readonly IPackStateService _packState;
        private readonly LoadedMarkerPack _workspacePack;
        private readonly Marker _addedMarker;

        public ActionType Type => ActionType.AddMarker;

        public AddMarkerAction(IPackStateService packState, LoadedMarkerPack workspacePack, Marker markerToAdd)
        {
            _packState = packState;
            _workspacePack = workspacePack;
            _addedMarker = markerToAdd;
        }

        public void Execute()
        {
            var parentCategory = FindParentCategory();
            parentCategory.Markers.Add(_addedMarker);
            _workspacePack.AddedMarkers.Add(_addedMarker);

            if (_packState is PackStateService service)
            {
                service.RaiseMarkerAdded(_addedMarker);
            }
        }

        public void Undo()
        {
            var parentCategory = FindParentCategory();
            parentCategory.Markers.Remove(_addedMarker);
            _workspacePack.AddedMarkers.Remove(_addedMarker);

            if (_packState is PackStateService service)
            {
                service.RaiseMarkerDeleted(_addedMarker);
            }
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
}
