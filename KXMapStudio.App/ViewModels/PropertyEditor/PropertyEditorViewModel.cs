using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using KXMapStudio.App.State;
using KXMapStudio.Core;

namespace KXMapStudio.App.ViewModels.PropertyEditor;

public partial class PropertyEditorViewModel : ObservableObject
{
    private readonly IPackStateService _packState;
    private ObservableCollection<Marker> SelectedMarkers => _packState.SelectedMarkers;
    private const string MultipleValuesPlaceholder = "<multiple values>";

    public PropertyEditorViewModel(IPackStateService packStateService)
    {
        _packState = packStateService;
        HookSelectionEvents();
    }

    private void HookSelectionEvents()
    {
        SelectedMarkers.CollectionChanged += OnSelectionChanged;
        _packState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IPackStateService.SelectedMarkers))
            {
                OnSelectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        };
    }

    private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var marker in e.OldItems.OfType<Marker>())
            {
                marker.PropertyChanged -= OnSelectedMarkerPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var marker in e.NewItems.OfType<Marker>())
            {
                marker.PropertyChanged += OnSelectedMarkerPropertyChanged;
            }
        }

        OnPropertyChanged(string.Empty);
    }

    private void OnSelectedMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    public bool IsSingleMarkerSelected => SelectedMarkers.Count == 1;

    public string? MapId
    {
        get => GetUnifiedValue(m => m.MapId.ToString());
        set
        {
            if (value != null && int.TryParse(value, out var parsedValue))
            {
                foreach (var marker in SelectedMarkers)
                {
                    marker.MapId = parsedValue;
                }
            }
        }
    }

    public string? X
    {
        get => GetUnifiedValue(m => m.X.ToString("F3", CultureInfo.InvariantCulture));
        set
        {
            if (value != null && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                foreach (var marker in SelectedMarkers)
                {
                    marker.X = parsedValue;
                }
            }
        }
    }

    public string? Y
    {
        get => GetUnifiedValue(m => m.Y.ToString("F3", CultureInfo.InvariantCulture));
        set
        {
            if (value != null && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                foreach (var marker in SelectedMarkers)
                {
                    marker.Y = parsedValue;
                }
            }
        }
    }

    public string? Z
    {
        get => GetUnifiedValue(m => m.Z.ToString("F3", CultureInfo.InvariantCulture));
        set
        {
            if (value != null && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                foreach (var marker in SelectedMarkers)
                {
                    marker.Z = parsedValue;
                }
            }
        }
    }

    public string? Type
    {
        get => GetUnifiedValue(m => m.Type);
        set
        {
            if (!string.IsNullOrEmpty(value) && value != MultipleValuesPlaceholder)
            {
                foreach (var marker in SelectedMarkers)
                {
                    marker.Type = value;
                }
            }
        }
    }

    public string? GuidFormatted => IsSingleMarkerSelected ? SelectedMarkers.First().GuidFormatted : null;

    public string? SourceFile
        => GetUnifiedValue(m => m.SourceFile);

    private string? GetUnifiedValue(Func<Marker, string?> selector)
    {
        if (SelectedMarkers.Count == 0)
        {
            return null;
        }

        var firstValue = selector(SelectedMarkers.First());
        return SelectedMarkers.All(m => string.Equals(selector(m), firstValue, StringComparison.OrdinalIgnoreCase))
            ? firstValue
            : MultipleValuesPlaceholder;
    }
}
