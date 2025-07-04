using KXMapStudio.Core;

using Microsoft.Extensions.Logging;

using System; // Required for Math.Sqrt

namespace KXMapStudio.App.Services;

public class WaypointFinderService
{
    private readonly MapDataService _mapDataService;
    private readonly CoordinateConverterService _coordinateConverterService;
    private readonly ILogger<WaypointFinderService> _logger;

    public WaypointFinderService(MapDataService mapDataService, CoordinateConverterService coordinateConverterService, ILogger<WaypointFinderService> logger)
    {
        _mapDataService = mapDataService;
        _coordinateConverterService = coordinateConverterService;
        _logger = logger;
    }

    public Waypoint? FindNearestWaypoint(Marker customMarker)
    {
        // 1. Get Waypoints for the specific map
        var waypoints = _mapDataService.GetWaypointsForMap(customMarker.MapId);
        if (waypoints is null || waypoints.Count == 0)
        {
            _logger.LogInformation("No waypoints found for MapId: {MapId}", customMarker.MapId);
            return null;
        }

        // 2. Convert custom marker's world coordinates to continent coordinates
        CoordinateConverterService.Point2D? markerContinentCoords = null;
        try
        {
            // IMPORTANT: Call the corrected conversion method
            markerContinentCoords = _coordinateConverterService.ConvertWorldToContinentCoordinates(customMarker);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to convert marker coordinates for MapId {MapId}. Map data might be incomplete.", customMarker.MapId);
            return null; // Cannot proceed if conversion fails
        }

        if (markerContinentCoords == null)
        {
            _logger.LogWarning("Coordinate conversion returned null for marker on MapId: {MapId}. Map data might be incomplete.", customMarker.MapId);
            return null;
        }

        // 3. Find the nearest waypoint
        Waypoint? nearestWaypoint = null;
        double minDistanceSquared = double.MaxValue;

        foreach (var waypoint in waypoints)
        {
            // Waypoint.Coord contains continent coordinates [X, Y] already
            double dx = markerContinentCoords.X - waypoint.Coord[0];
            double dy = markerContinentCoords.Y - waypoint.Coord[1];

            double distanceSquared = dx * dx + dy * dy; // Avoid sqrt for comparison performance

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                nearestWaypoint = waypoint;
            }
        }

        // Optional: Log the found waypoint if useful for debugging
        if (nearestWaypoint != null)
        {
            _logger.LogDebug("Nearest waypoint found for marker on MapId {MapId}: {WaypointName} (ID: {WaypointId})",
                              customMarker.MapId, nearestWaypoint.Name, nearestWaypoint.Id);
        }

        return nearestWaypoint;
    }
}
