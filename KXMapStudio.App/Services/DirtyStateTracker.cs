using System.Collections.ObjectModel;
using System.Collections.Specialized;

using KXMapStudio.Core;
using KXMapStudio.App.State;

namespace KXMapStudio.App.Services
{
    public class DirtyStateTracker : IDirtyStateTracker
    {
        private IPackStateService? _packState;

        public void StartTracking(ObservableCollection<Marker> markers, IPackStateService packState)
        {
            _packState = packState;
            markers.CollectionChanged += OnMarkersCollectionChanged;
            foreach (var marker in markers)
            {
                marker.PropertyChanged += OnMarkerPropertyChanged;
            }
        }

        public void StopTracking(ObservableCollection<Marker> markers)
        {
            markers.CollectionChanged -= OnMarkersCollectionChanged;
            foreach (var marker in markers)
            {
                marker.PropertyChanged -= OnMarkerPropertyChanged;
            }
            _packState = null;
        }

        private void OnMarkersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_packState == null)
            {
                return;
            }

            if (e.OldItems != null)
            {
                foreach (var marker in e.OldItems.OfType<Marker>())
                {
                    marker.PropertyChanged -= OnMarkerPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var marker in e.NewItems.OfType<Marker>())
                {
                    marker.PropertyChanged += OnMarkerPropertyChanged;
                }
            }
            // Any collection change (add/remove) makes the document dirty
            _packState.UpdateDirtyState();
        }

        private void OnMarkerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_packState == null)
            {
                return;
            }

            if (e.PropertyName == nameof(Marker.IsDirty))
            {
                // Only update if the dirty state actually changed and it's for the active document
                if (sender is Marker marker && marker.SourceFile.Equals(_packState.ActiveDocumentPath, StringComparison.OrdinalIgnoreCase))
                {
                    _packState.UpdateDirtyState();
                }
            }
        }
    }
}
