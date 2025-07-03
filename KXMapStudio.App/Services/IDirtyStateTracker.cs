using System.Collections.ObjectModel;
using KXMapStudio.Core;
using KXMapStudio.App.State;

namespace KXMapStudio.App.Services
{
    public interface IDirtyStateTracker
    {
        void StartTracking(ObservableCollection<Marker> markers, IPackStateService packState);
        void StopTracking(ObservableCollection<Marker> markers);
    }
}