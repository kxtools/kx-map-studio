using System.IO;

namespace KXMapStudio.App.Services.Pack;

public class PackLoaderFactory
{
    public IPackLoader GetLoader(string path)
    {
        if (File.Exists(path))
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".taco" or ".zip")
            {
                return new ArchivePackLoader();
            }
            else if (extension == ".xml")
            {
                return new SingleFilePackLoader();
            }
        }
        else if (Directory.Exists(path))
        {
            return new DirectoryPackLoader();
        }

        throw new FileNotFoundException("The specified pack, file, or directory was not found.", path);
    }
}
