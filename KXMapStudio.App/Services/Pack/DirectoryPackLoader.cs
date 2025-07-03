using KXMapStudio.Core;

using System.IO;

namespace KXMapStudio.App.Services.Pack;

public class DirectoryPackLoader : IPackLoader
{
    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public DirectoryPackLoader(MarkerXmlParser markerXmlParser, CategoryBuilder categoryBuilder)
    {
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(path, file).Replace('\\', '/');
            originalRawContent[relativePath] = await File.ReadAllBytesAsync(file);
        }

        var packLoader = new PackLoader(_markerXmlParser, _categoryBuilder);
        return await packLoader.LoadPackFromMemoryAsync(originalRawContent, path, false);
    }
}
