namespace KXMapStudio.Libs.Models.LeftSide.WorkshopExplorer;

/// <summary>
///     Represents a UI-bindable tree node for the Workshop Explorer.
/// </summary>
/// <remarks>
///     <para>
///         This model supports hierarchical display of files and directories with observable properties
///         for UI state (expanded, selected, modified).
///     </para>
///     <para>
///         Uses CommunityToolkit.Mvvm source generators for efficient property change notifications.
///     </para>
/// </remarks>
public sealed partial class WorkshopExplorerNodeModel : ObservableObject
{
	/// <summary>
	///     Gets or sets a value indicating whether the node is expanded in the tree view.
	/// </summary>
	[ObservableProperty] private bool _isExpanded;

	/// <summary>
	///     Gets or sets a value indicating whether the node content has been modified.
	/// </summary>
	[ObservableProperty] private bool _isModified;

	/// <summary>
	///     Gets or sets a value indicating whether the node is currently selected.
	/// </summary>
	[ObservableProperty] private bool _isSelected;

	/// <summary>
	///     Initializes a new instance of the <see cref="WorkshopExplorerNodeModel" /> class.
	/// </summary>
	/// <param name="name">The display name of the node.</param>
	/// <param name="fullPath">The full filesystem path of the node.</param>
	/// <param name="isDirectory">A value indicating whether this node represents a directory.</param>
	public WorkshopExplorerNodeModel(string name, string fullPath, bool isDirectory)
	{
		Name = name;
		FullPath = fullPath;
		IsDirectory = isDirectory;
		Extension = isDirectory ? string.Empty : Path.GetExtension(fullPath);
		Children = [];
	}

	/// <summary>
	///     Gets the display name of the node.
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     Gets the full filesystem path of the node.
	/// </summary>
	public string FullPath { get; }

	/// <summary>
	///     Gets a value indicating whether this node represents a directory.
	/// </summary>
	public bool IsDirectory { get; }

	/// <summary>
	///     Gets the file extension (empty for directories).
	/// </summary>
	public string Extension { get; }

	/// <summary>
	///     Gets the collection of child nodes.
	/// </summary>
	public ObservableCollection<WorkshopExplorerNodeModel> Children { get; }
}