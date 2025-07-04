using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

/// <summary>
/// Converts world coordinates (meters) from MumbleLink into continent pixel coordinates that match
/// the "coord" arrays returned by /v2/continents and /v2/maps. This is a direct C# port of the logic
/// that Blish HUD uses (WorldUtil.WorldToGameCoord + MapToContinent) and has been verified by
/// comparing its output with the live <c>PlayerLocationMap</c> values for multiple waypoints.
/// </summary>
public sealed class CoordinateConverterService
{
    private const double InchesPerMeter = 39.37007874015748; // 1 / 0.0254f

    private readonly MapDataService _mapDataService;

    public CoordinateConverterService(MapDataService mapDataService)
    {
        _mapDataService = mapDataService ?? throw new ArgumentNullException(nameof(mapDataService));
    }

    /// <summary>
    /// Convert the supplied marker's world (X,Z) position to continent (X,Y).
    /// </summary>
    public Point2D ConvertWorldToContinentCoordinates(Marker marker)
    {
        var mapData = _mapDataService.GetMapData(marker.MapId)
                     ?? throw new InvalidOperationException($"Geometry missing for map {marker.MapId}.");

        // 1) World metres → map inches
        double mapX = marker.X * InchesPerMeter;
        double mapY = marker.Z * InchesPerMeter; // Z grows north in GW2 world space, matches map Y so no sign flip

        // 2) Normalise to 0..1 inside map_rect
        var mapMinX = mapData.MapRect[0][0];
        var mapMinY = mapData.MapRect[0][1];
        var mapMaxX = mapData.MapRect[1][0];
        var mapMaxY = mapData.MapRect[1][1];

        double pctX = (mapX - mapMinX) / (mapMaxX - mapMinX);
        double pctY = (mapY - mapMinY) / (mapMaxY - mapMinY);

        // 3) Interpolate into continent_rect, **inverting the Y axis** because the continent sheet origin is top‑left
        var contMinX = mapData.ContinentRect[0][0];
        var contMinY = mapData.ContinentRect[0][1];
        var contMaxX = mapData.ContinentRect[1][0];
        var contMaxY = mapData.ContinentRect[1][1];

        double continentX = contMinX + pctX * (contMaxX - contMinX);
        double continentY = contMinY + (1d - pctY) * (contMaxY - contMinY);

        return new Point2D(Math.Round(continentX, 1), Math.Round(continentY, 1));
    }
}
