namespace KXMapStudio.Core.Abstractions.Services.Validation;

/// <summary>
///     Defines the contract for file validation operations.
/// </summary>
/// <remarks>
///     This service provides abstracted file validation logic, maintaining the
///     separation between business rules and direct file system access.
/// </remarks>
public interface IFileValidationService
{
	/// <summary>
	///     Validates whether a file exists at the specified path.
	/// </summary>
	/// <param name="filePath">The absolute file path to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the file exists;
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="filePath" /> is <see langword="null" /> or whitespace.
	/// </exception>
	bool FileExists(string filePath);

	/// <summary>
	///     Validates whether a directory exists at the specified path.
	/// </summary>
	/// <param name="directoryPath">The absolute directory path to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the directory exists;
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="directoryPath" /> is <see langword="null" /> or whitespace.
	/// </exception>
	bool DirectoryExists(string directoryPath);

	/// <summary>
	///     Validates whether the specified file path has a JSON extension.
	/// </summary>
	/// <param name="filePath">The file path to validate.</param>
	/// <returns>
	///     <see langword="true" /> if the file has a .json extension;
	///     otherwise, <see langword="false" />.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="filePath" /> is <see langword="null" /> or whitespace.
	/// </exception>
	bool IsJsonFile(string filePath);
}