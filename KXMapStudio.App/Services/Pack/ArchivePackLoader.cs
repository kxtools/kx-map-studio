using KXMapStudio.Core;

using System.IO;
using System.IO.Compression;

namespace KXMapStudio.App.Services.Pack;

public class ArchivePackLoader : IPackLoader
{
    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public ArchivePackLoader(MarkerXmlParser markerXmlParser, CategoryBuilder categoryBuilder)
    {
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using (var archive = ZipFile.OpenRead(path))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var entryFullName = entry.FullName.Replace('\\', '/');
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                originalRawContent[entryFullName] = ms.ToArray();
            }
        }

        var packLoader = new PackLoader(_markerXmlParser, _categoryBuilder);
        return await packLoader.LoadPackFromMemoryAsync(originalRawContent, path, true);
    }
}
