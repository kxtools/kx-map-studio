using System.IO;

namespace KXMapStudio.App.Services.Pack;

public class PackLoaderFactory
{
    private readonly MarkerXmlParser _markerXmlParser;
    private readonly CategoryBuilder _categoryBuilder;

    public PackLoaderFactory(MarkerXmlParser markerXmlParser, CategoryBuilder categoryBuilder)
    {
        _markerXmlParser = markerXmlParser;
        _categoryBuilder = categoryBuilder;
    }

    public IPackLoader GetLoader(string path)
    {
        if (File.Exists(path))
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".taco" or ".zip")
            {
                return new ArchivePackLoader(_markerXmlParser, _categoryBuilder);
            }
            else if (extension == ".xml")
            {
                return new SingleFilePackLoader(_markerXmlParser, _categoryBuilder);
            }
        }
        else if (Directory.Exists(path))
        {
            return new DirectoryPackLoader(_markerXmlParser, _categoryBuilder);
        }

        throw new FileNotFoundException("The specified pack, file, or directory was not found.", path);
    }
}
