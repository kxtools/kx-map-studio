# This script fetches data from the Guild Wars 2 API and pre-processes it into JSON files
# for use by the KXMapStudio application.

# Define output paths relative to the script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$appDir = Join-Path $scriptDir "KXMapStudio.App"
$outputWaypointsFile = Join-Path $appDir "AllWaypoints.json"
$outputMapRectsFile = Join-Path $appDir "AllMapRects.json"

# Ensure the output directory exists
if (-not (Test-Path $appDir)) {
    Write-Host "Error: KXMapStudio.App directory not found at $appDir. Please ensure the script is run from the repository root."
    exit 1
}

Write-Host "Fetching continent data from /v2/continents/1/floors/1..."
try {
    $continentData = Invoke-RestMethod -Uri "https://api.guildwars2.com/v2/continents/1/floors/1"
} catch {
    Write-Host "Error fetching continent data: $($_.Exception.Message)"
    exit 1
}

Write-Host "Processing continent data and extracting waypoints..."
$allWaypoints = @()
foreach ($regionEntry in $continentData.regions.PSObject.Properties) {
    foreach ($mapEntry in $regionEntry.Value.maps.PSObject.Properties) {
        $mapId = [int]$mapEntry.Name # Map IDs are string keys in the JSON
        $map = $mapEntry.Value

        if ($map.points_of_interest) {
            foreach ($poiEntry in $map.points_of_interest.PSObject.Properties) {
                $poi = $poiEntry.Value
                if ($poi.type -eq 'waypoint') {
                    $waypointObject = [PSCustomObject]@{ # Matches C# Waypoint class
                        Id              = $poi.id
                        Name            = $poi.name
                        Type            = $poi.type
                        Coord           = $poi.coord
                        ChatLink        = $poi.chat_link
                        MapId           = $mapId
                    }
                    $allWaypoints += $waypointObject
                }
            }
        }
    }
}

Write-Host "Found $($allWaypoints.Count) waypoints. Saving to $outputWaypointsFile..."
$allWaypoints | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputWaypointsFile -Encoding utf8

Write-Host "Fetching map data from /v2/maps?ids=all..."
try {
    $mapsData = Invoke-RestMethod -Uri "https://api.guildwars2.com/v2/maps?ids=all"
} catch {
    Write-Host "Error fetching map data: $($_.Exception.Message)"
    exit 1
}

Write-Host "Processing map data and extracting map rectangles..."
$allMapRectsList = @()
foreach ($map in $mapsData) {
    $mapObject = [PSCustomObject]@{ # Matches C# Map class
        Id           = $map.id
        Name         = $map.name
        MapRect      = $map.map_rect
        ContinentRect = $map.continent_rect
    }
    $allMapRectsList += $mapObject
}

Write-Host "Found $($allMapRectsList.Count) map rectangles. Saving to $outputMapRectsFile..."
$allMapRectsList | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputMapRectsFile -Encoding utf8

Write-Host "Data generation complete. Please ensure these files are included as embedded resources in KXMapStudio.App.csproj."
