namespace KXMapStudio.Libs.Abstractions.Services;

/// <summary>
///     Tracks the currently open document in the workspace.
/// </summary>
/// <remarks>
///     This service provides a lightweight abstraction for components that need to know
///     which file is currently open without depending on the full editor ViewModel.
///     This maintains separation of concerns and reduces coupling.
/// </remarks>
public interface IOpenDocumentTracker
{
	/// <summary>
	///     Gets the full path of the currently open document, or <see langword="null" /> if no document is open.
	/// </summary>
	string? CurrentOpenFilePath { get; }

	/// <summary>
	///     Gets a value indicating whether a document is currently loaded.
	/// </summary>
	bool HasOpenDocument { get; }

	/// <summary>
	///     Checks if the specified file path is currently open.
	/// </summary>
	/// <param name="filePath">The file path to check.</param>
	/// <returns><see langword="true" /> if the file is currently open; otherwise, <see langword="false" />.</returns>
	bool IsFileOpen(string filePath);
}