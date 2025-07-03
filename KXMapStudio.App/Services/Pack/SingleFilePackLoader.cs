using KXMapStudio.Core;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KXMapStudio.App.Services.Pack;

public class SingleFilePackLoader : IPackLoader
{
    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public SingleFilePackLoader(MarkerXmlParser markerXmlParser, CategoryBuilder categoryBuilder)
    {
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        originalRawContent[Path.GetFileName(path)] = await File.ReadAllBytesAsync(path);

        var packLoader = new PackLoader(_markerXmlParser, _categoryBuilder);
        return await packLoader.LoadPackFromMemoryAsync(originalRawContent, path, false);
    }
}
