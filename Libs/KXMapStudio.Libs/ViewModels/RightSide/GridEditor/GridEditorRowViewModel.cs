namespace KXMapStudio.Libs.ViewModels.RightSide.GridEditor;

/// <summary>
///     Represents a single editable grid row in the coordinate editor, with observable properties for UI binding.
/// </summary>
/// <remarks>
///     This ViewModel uses CommunityToolkit.Mvvm source generators (<c>[ObservableProperty]</c>)
///     to generate property change notifications automatically, ensuring efficient UI updates.
/// </remarks>
public sealed partial class GridEditorRowViewModel : ObservableObject
{
	/// <summary>
	///     Gets or sets the unique identifier (1-based index) of the row.
	/// </summary>
	[ObservableProperty] private int _id;

	/// <summary>
	///     Gets or sets the display name or label for the coordinate marker.
	/// </summary>
	[ObservableProperty] private string _name = string.Empty;

	/// <summary>
	///     Gets or sets the X coordinate value (horizontal position).
	/// </summary>
	[ObservableProperty] private double _x;

	/// <summary>
	///     Gets or sets the Y coordinate value (vertical position).
	/// </summary>
	[ObservableProperty] private double _y;

	/// <summary>
	///     Gets or sets the Z coordinate value (elevation/depth).
	/// </summary>
	[ObservableProperty] private double _z;
}