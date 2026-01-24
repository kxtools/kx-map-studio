namespace KXMapStudio.Libs.Models.Grid;

/// <summary>
///     Represents coordinate data for a grid row, independent of UI concerns.
/// </summary>
/// <param name="Name">The display name or label for the coordinate marker.</param>
/// <param name="X">The X coordinate value (horizontal position).</param>
/// <param name="Y">The Y coordinate value (vertical position).</param>
/// <param name="Z">The Z coordinate value (elevation/depth).</param>
/// <remarks>
///     This is a data-only record that can be used by services without coupling to ViewModels.
///     It serves as a lightweight DTO between the domain layer and presentation layer.
/// </remarks>
public sealed record GridRowData(
	string Name,
	double X,
	double Y,
	double Z);