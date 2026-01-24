namespace KXMapStudio.Core.Types.Structs;

/// <summary>
///     Defines file extension constants used by KXMapStudio.
/// </summary>
public readonly ref struct FileExtension
{
	/// <summary>
	///     The <c>.json</c> file extension.
	/// </summary>
	public const string Json = ".json";

	/// <summary>
	///     Gets the set of file extensions supported by Core file workflows.
	/// </summary>
	/// <remarks>
	///     The comparer is case-insensitive on purpose.
	/// </remarks>
	public static ISet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		Json
	};
}