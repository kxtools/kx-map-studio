using System;

namespace KXMapStudio.App.Services
{
    public class CoordinateConverterService
    {
        public double[] ConvertMapToContinentCoordinates(double x, double y, double[] mapRect, double[] continentRect)
        {
            var mapWidth = mapRect[1] - mapRect[0];
            var mapHeight = mapRect[3] - mapRect[2];

            var continentWidth = continentRect[1] - continentRect[0];
            var continentHeight = continentRect[3] - continentRect[2];

            var xPercent = (x - mapRect[0]) / mapWidth;
            var yPercent = (y - mapRect[2]) / mapHeight;

            var continentX = continentRect[0] + (continentWidth * xPercent);
            var continentY = continentRect[2] + (continentHeight * yPercent);

            return new[] { continentX, continentY };
        }
    }
}
