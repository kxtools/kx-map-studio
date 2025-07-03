using System.Collections.ObjectModel;

using KXMapStudio.App.Services;
using KXMapStudio.Core;

namespace KXMapStudio.App.Actions
{
    public enum ReorderDirection
    {
        Up,
        Down
    }

    public class ReorderMarkersAction : IAction
    {
        private readonly ObservableCollection<Marker> _collection;
        private readonly List<Marker> _markersToMove;
        private readonly ReorderDirection _direction;

        public ActionType Type => ActionType.ReorderMarkers;

        public ReorderMarkersAction(ObservableCollection<Marker> collection, IEnumerable<Marker> markersToMove, ReorderDirection direction)
        {
            _collection = collection;
            _markersToMove = markersToMove.ToList();
            _direction = direction;
        }

        public bool Execute()
        {
            PerformMove(_direction);
            return true;
        }

        public bool Undo()
        {
            var undoDirection = _direction == ReorderDirection.Up ? ReorderDirection.Down : ReorderDirection.Up;
            PerformMove(undoDirection);
            return true;
        }

        private void PerformMove(ReorderDirection direction)
        {
            if (direction == ReorderDirection.Up)
            {
                var sortedMarkers = _markersToMove.OrderBy(m => _collection.IndexOf(m)).ToList();
                foreach (var marker in sortedMarkers)
                {
                    int oldIndex = _collection.IndexOf(marker);
                    if (oldIndex > 0)
                    {
                        _collection.Move(oldIndex, oldIndex - 1);
                    }
                }
            }
            else
            {
                var sortedMarkers = _markersToMove.OrderByDescending(m => _collection.IndexOf(m)).ToList();
                foreach (var marker in sortedMarkers)
                {
                    int oldIndex = _collection.IndexOf(marker);
                    if (oldIndex < _collection.Count - 1)
                    {
                        _collection.Move(oldIndex, oldIndex + 1);
                    }
                }
            }
        }
    }
}
