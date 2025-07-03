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
        private readonly int _insertionIndex;
        private readonly Category _parentCategory;

        // The ActionType can remain AddMarker for both cases.
        public ActionType Type => ActionType.AddMarker;

        /// <summary>
        /// Creates an action to add a marker.
        /// </summary>
        /// <param name="packState">The pack state service.</param>
        /// <param name="workspacePack">The loaded marker pack.</param>
        /// <param name="markerToAdd">The marker to add.</param>
        /// <param name="insertionIndex">The index to insert the marker at. If -1, the marker is appended to the end.</param>
        public AddMarkerAction(IPackStateService packState, LoadedMarkerPack workspacePack, Marker markerToAdd, int insertionIndex = -1)
        {
            _packState = packState;
            _workspacePack = workspacePack;
            _addedMarker = markerToAdd;
            _insertionIndex = insertionIndex;
            _parentCategory = FindParentCategory();
        }

        public void Execute()
        {
            // 1. Modify the model's category tree. For simplicity, we always append here.
            // Preserving insertion order in categories is a more complex feature.
            _parentCategory.Markers.Add(_addedMarker);

            // 2. Modify the model's file list at the correct index.
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

            // 3. Modify the model's "dirty" state
            _workspacePack.AddedMarkers.Add(_addedMarker);

            // 4. Announce that the model has changed
            if (_packState is PackStateService service)
            {
                service.RaiseMarkerAdded(_addedMarker);
            }
        }

        public void Undo()
        {
            _parentCategory.Markers.Remove(_addedMarker);

            if (_workspacePack.MarkersByFile.TryGetValue(_addedMarker.SourceFile, out var markersForFile))
            {
                markersForFile.Remove(_addedMarker);
            }

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
