using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public class InsertMarkerAction : IAction
    {
        private readonly IPackStateService _packState;
        private readonly LoadedMarkerPack _workspacePack;
        private readonly Marker _markerToInsert;
        private readonly int _insertionIndex;

        public ActionType Type => ActionType.InsertMarker;

        public InsertMarkerAction(IPackStateService packState, LoadedMarkerPack workspacePack, Marker markerToInsert, int insertionIndex)
        {
            _packState = packState;
            _workspacePack = workspacePack;
            _markerToInsert = markerToInsert;
            _insertionIndex = insertionIndex;
        }

        public void Execute()
        {
            _packState.ActiveDocumentMarkers.Insert(_insertionIndex, _markerToInsert);
            _workspacePack.AddedMarkers.Add(_markerToInsert);

            if (_packState is PackStateService service)
            {
                service.RaiseMarkerAdded(_markerToInsert);
            }
        }

        public void Undo()
        {
            _packState.ActiveDocumentMarkers.RemoveAt(_insertionIndex);
            _workspacePack.AddedMarkers.Remove(_markerToInsert);

            if (_packState is PackStateService service)
            {
                service.RaiseMarkerDeleted(_markerToInsert);
            }
        }
    }
}
