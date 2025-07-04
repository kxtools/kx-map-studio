using KXMapStudio.Core;

using System; // Required for Math.Round

namespace KXMapStudio.App.Services
{
    // A simple record to hold the 2D continent coordinates.
    // Making it public because it's returned by this service
    public record Point2D(double X, double Y);

    public class CoordinateConverterService
    {
        private readonly MapDataService _mapDataService;

        public CoordinateConverterService(MapDataService mapDataService)
        {
            _mapDataService = mapDataService;
        }

        /// <summary>
        /// Converts a marker's 3D world coordinates (from Mumble Link) to 2D continent coordinates (API map coordinates).
        /// This uses the transformation rectangles provided by the Guild Wars 2 API.
        /// </summary>
        /// <param name="marker">The marker with world X, Y, Z coordinates and MapId.</param>
        /// <returns>A Point2D record representing the continent coordinates, or null if map data is missing.</returns>
        public Point2D? ConvertWorldToContinentCoordinates(Marker marker)
        {
            var mapData = _mapDataService.GetMapData(marker.MapId);
            if (mapData == null || mapData.MapRect.Length < 2 || mapData.ContinentRect.Length < 2)
            {
                // Map data (rects) not available or malformed for this map.
                // This scenario should be rare if AllMapRects.json is complete.
                return null;
            }

            // World coordinates from Marker. Mumble Link's X is map X, Mumble Link's Z is map Y.
            double worldX = marker.X;
            double worldY = marker.Z; // IMPORTANT: Mumble's Z is the Y-axis on the 2D map

            // MapRect: [[minX, minY], [maxX, maxY]] in game world coordinates (units usually 1/39.37 of inches in Mumble Link)
            // However, the conversion is proportional based on these rects, so direct Mumble units are fine relative to MapRect.
            double mapMinX = mapData.MapRect[0][0];
            double mapMinY = mapData.MapRect[0][1];
            double mapMaxX = mapData.MapRect[1][0];
            double mapMaxY = mapData.MapRect[1][1];

            // ContinentRect: [[minX, minY], [maxX, maxY]] in continent pixels
            double continentMinX = mapData.ContinentRect[0][0];
            double continentMinY = mapData.ContinentRect[0][1];
            double continentMaxX = mapData.ContinentRect[1][0];
            double continentMaxY = mapData.ContinentRect[1][1];

            // Calculate percentage within the map_rect
            // Guard against division by zero if mapWidth/Height is somehow 0 (unlikely for valid GW2 maps)
            double percentX = (mapMaxX - mapMinX != 0) ? (worldX - mapMinX) / (mapMaxX - mapMinX) : 0;
            double percentY = (mapMaxY - mapMinY != 0) ? (worldY - mapMinY) / (mapMaxY - mapMinY) : 0;

            // Apply percentage to continent_rect to get the final coordinate
            double continentX = continentMinX + (percentX * (continentMaxX - continentMinX));
            double continentY = continentMinY + (percentY * (continentMaxY - continentMinY));

            // Round to a reasonable precision, as these are pixel-like coordinates
            return new Point2D(Math.Round(continentX, 4), Math.Round(continentY, 4));
        }
    }
}
