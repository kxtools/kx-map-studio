namespace KXMapStudio.Libs.Models.LeftSide.WorkshopExplorer;

/// <summary>
///     Immutable scan result node representing a directory or a file.
/// </summary>
public sealed record WorkshopExplorerScanNode(
	string Name,
	string FullPath,
	bool IsDirectory,
	IReadOnlyList<WorkshopExplorerScanNode> Children);