namespace KXMapStudio.Libs.Models.Editor;

/// <summary>
///     Specifies the source kind of an editor document.
/// </summary>
public enum EditorDocumentSourceKind
{
	/// <summary>
	///     The document is a workspace file stored on the filesystem.
	/// </summary>
	WorkspaceFile = 0,

	/// <summary>
	///     The document is an entry embedded within an archive (ZIP/TACO).
	/// </summary>
	ArchiveEntry = 1
}