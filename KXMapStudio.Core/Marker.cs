using CommunityToolkit.Mvvm.ComponentModel;

namespace KXMapStudio.Core;

public partial class Marker : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GuidFormatted))]
    private Guid _guid;

    [ObservableProperty]
    private int _mapId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XFormatted))]
    private double _x;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YFormatted))]
    private double _y;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZFormatted))]
    private double _z;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _sourceFile = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    public string GuidFormatted => Guid.ToString("N");
    public string XFormatted => X.ToString("F3");
    public string YFormatted => Y.ToString("F3");
    public string ZFormatted => Z.ToString("F3");

    private bool _isChangeTrackingEnabled = false;

    public Marker()
    {
    }

    /// <summary>
    /// Enables change tracking for the marker, allowing IsDirty to be set.
    /// </summary>
    public void EnableChangeTracking()
    {
        _isChangeTrackingEnabled = true;
    }

    partial void OnMapIdChanged(int value)
    {
        if (_isChangeTrackingEnabled)
        {
            IsDirty = true;
        }
    }
    partial void OnXChanged(double value)
    {
        if (_isChangeTrackingEnabled)
        {
            IsDirty = true;
        }
    }
    partial void OnYChanged(double value)
    {
        if (_isChangeTrackingEnabled)
        {
            IsDirty = true;
        }
    }
    partial void OnZChanged(double value)
    {
        if (_isChangeTrackingEnabled)
        {
            IsDirty = true;
        }
    }
    partial void OnTypeChanged(string value)
    {
        if (_isChangeTrackingEnabled)
        {
            IsDirty = true;
        }
    }
}
