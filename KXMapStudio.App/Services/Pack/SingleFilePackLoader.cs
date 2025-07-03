using KXMapStudio.Core;

using System.IO;

namespace KXMapStudio.App.Services.Pack;

public class SingleFilePackLoader : IPackLoader
{
    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        originalRawContent[Path.GetFileName(path)] = await File.ReadAllBytesAsync(path);

        var packLoader = new PackLoader();
        return await packLoader.LoadPackFromMemoryAsync(originalRawContent, path, false);
    }
}
