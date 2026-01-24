namespace KXMapStudio.Core.Services.Validation;

/// <summary>
///     Provides file validation operations with proper abstraction over file system access.
/// </summary>
/// <remarks>
///     This service maintains the architectural boundary by providing an abstraction layer
///     between business logic and direct file system operations.
/// </remarks>
public sealed class FileValidationService : IFileValidationService
{
	/// <inheritdoc />
	public bool FileExists(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		return File.Exists(filePath);
	}

	/// <inheritdoc />
	public bool DirectoryExists(string directoryPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

		return Directory.Exists(directoryPath);
	}

	/// <inheritdoc />
	public bool IsJsonFile(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		var extension = Path.GetExtension(filePath);
		return string.Equals(extension, FileExtension.Json, StringComparison.OrdinalIgnoreCase);
	}
}