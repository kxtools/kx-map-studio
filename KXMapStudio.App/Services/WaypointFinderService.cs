
using System;
using KXMapStudio.Core;

namespace KXMapStudio.App.Services;

public class WaypointFinderService
{
    private readonly MapDataService _mapDataService;
    private readonly CoordinateConverterService _coordinateConverterService;

    public WaypointFinderService(MapDataService mapDataService, CoordinateConverterService coordinateConverterService)
    {
        _mapDataService = mapDataService;
        _coordinateConverterService = coordinateConverterService;
    }

    public Waypoint? FindNearestWaypoint(Marker customMarker)
    {
        var waypoints = _mapDataService.GetWaypointsForMap(customMarker.MapId);
        if (waypoints is null || waypoints.Count == 0) return null;

        var mapData = _mapDataService.GetMapData(customMarker.MapId);
        if (mapData is null) return null;

        var markerContinentCoords = _coordinateConverterService.ConvertMapToContinentCoordinates(
            customMarker.X,
            customMarker.Y,
            mapData.MapRect.SelectMany(c => c).ToArray(),
            mapData.ContinentRect.SelectMany(c => c).ToArray());

        Waypoint? nearestWaypoint = null;
        double minDistanceSquared = double.MaxValue;

        foreach (var waypoint in waypoints)
        {
            double dx = markerContinentCoords[0] - waypoint.Coord[0];
            double dy = markerContinentCoords[1] - waypoint.Coord[1];

            double distanceSquared = dx * dx + dy * dy;

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                nearestWaypoint = waypoint;
            }
        }

        return nearestWaypoint;
    }
}
