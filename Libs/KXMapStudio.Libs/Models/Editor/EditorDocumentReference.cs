namespace KXMapStudio.Libs.Models.Editor;

public sealed record EditorDocumentReference(
	EditorDocumentSourceKind Kind,
	string DisplayName,
	string? FilePath,
	string? ArchivePath,
	string? ArchiveEntryFullName,
	string Extension)
{
	public bool IsArchiveEntry => Kind == EditorDocumentSourceKind.ArchiveEntry;
	public bool IsWorkspaceFile => Kind == EditorDocumentSourceKind.WorkspaceFile;

	public static EditorDocumentReference FromWorkspaceFile(string fullPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

		return new EditorDocumentReference(
			EditorDocumentSourceKind.WorkspaceFile,
			Path.GetFileName(fullPath),
			fullPath,
			null,
			null,
			Path.GetExtension(fullPath));
	}

	public static EditorDocumentReference FromArchiveEntry(string archivePath, string entryFullName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(entryFullName);

		var normalizedEntry = entryFullName.Replace('\\', '/');

		return new EditorDocumentReference(
			EditorDocumentSourceKind.ArchiveEntry,
			Path.GetFileName(normalizedEntry),
			null,
			archivePath,
			normalizedEntry,
			Path.GetExtension(normalizedEntry));
	}
}