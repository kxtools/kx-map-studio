using KXMapStudio.App.Services.Pack;
using KXMapStudio.Core;
using System.Threading.Tasks;

namespace KXMapStudio.App.Services;

public class PackService
{
    private readonly PackLoaderFactory _packLoaderFactory;

    public PackService(PackLoaderFactory packLoaderFactory)
    {
        _packLoaderFactory = packLoaderFactory;
    }

    public async Task<PackLoadResult> LoadPackAsync(string path)
    {
        var loader = _packLoaderFactory.GetLoader(path);
        return await loader.LoadPackAsync(path);
    }
}