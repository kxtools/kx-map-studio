using KXMapStudio.Core;

using System.IO;

namespace KXMapStudio.App.Services.Pack;

public class DirectoryPackLoader : IPackLoader
{
    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var originalRawContent = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(path, file).Replace('\\', '/');
            originalRawContent[relativePath] = await File.ReadAllBytesAsync(file);
        }

        var packLoader = new PackLoader();
        return await packLoader.LoadPackFromMemoryAsync(originalRawContent, path, false);
    }
}
