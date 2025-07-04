using KXMapStudio.Core;

using System;

namespace KXMapStudio.App.Services
{
    // Simple record returned by the converter
    public record Point2D(double X, double Y);

    /// <summary>
    /// Converts Mumble Link world coordinates (X,Z) into continent‑pixel coordinates
    /// that match the "coord" array supplied by the /v2/continents and /v2/maps endpoints.
    /// The math mirrors the proven implementation in Blish HUD (WorldUtil.cs).
    /// </summary>
    public sealed class CoordinateConverterService
    {
        private readonly MapDataService _mapDataService;

        public CoordinateConverterService(MapDataService mapDataService)
        {
            _mapDataService = mapDataService ?? throw new ArgumentNullException(nameof(mapDataService));
        }

        /// <summary>
        /// Converts the given marker's world position to continent pixels.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when geometry for the map is missing.</exception>
        public Point2D ConvertWorldToContinentCoordinates(Marker marker)
        {
            var mapData = _mapDataService.GetMapData(marker.MapId);
            if (mapData == null || mapData.MapRect.Length < 2 || mapData.ContinentRect.Length < 2)
            {
                throw new InvalidOperationException($"Geometry missing for map {marker.MapId}. Check AllMapRects.json.");
            }

            // world -> map. Game Z grows south, 2D map Y grows north, therefore negate Z when mapping to Y
            double mapX = marker.X;
            double mapY = -marker.Z;

            // Extract rectangles (min,max) for easier reading
            double mapMinX = mapData.MapRect[0][0];
            double mapMinY = mapData.MapRect[0][1];
            double mapMaxX = mapData.MapRect[1][0];
            double mapMaxY = mapData.MapRect[1][1];

            double continentMinX = mapData.ContinentRect[0][0];
            double continentMinY = mapData.ContinentRect[0][1];
            double continentMaxX = mapData.ContinentRect[1][0];
            double continentMaxY = mapData.ContinentRect[1][1];

            double percentX = (mapX - mapMinX) / (mapMaxX - mapMinX);
            double percentY = (mapY - mapMinY) / (mapMaxY - mapMinY);

            // Clamp the percentages in case the marker is slightly outside the playable rectangle
            percentX = Math.Clamp(percentX, 0d, 1d);
            percentY = Math.Clamp(percentY, 0d, 1d);

            double continentX = continentMinX + percentX * (continentMaxX - continentMinX);
            // Y axis is inverted in continent space (origin is top left)
            double continentY = continentMinY + (1d - percentY) * (continentMaxY - continentMinY);

            return new Point2D(Math.Round(continentX, 4), Math.Round(continentY, 4));
        }
    }
}
