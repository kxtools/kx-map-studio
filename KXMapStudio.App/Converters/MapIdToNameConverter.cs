using System.Globalization;
using System.Windows.Data;

using KXMapStudio.App.Services;

using Microsoft.Extensions.DependencyInjection;

namespace KXMapStudio.App.Converters;

public class MapIdToNameConverter : IValueConverter
{
    // Lazy-load the service to avoid issues with design-time environments.
    private readonly Lazy<MapDataService> _mapDataService = new(() =>
        App.AppHost!.Services.GetRequiredService<MapDataService>());

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int mapId)
        {
            return _mapDataService.Value.GetMapName(mapId);
        }
        return "Invalid ID";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
