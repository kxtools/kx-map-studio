using KXMapStudio.Core;

namespace KXMapStudio.App.Services.Pack;

public interface IPackLoader
{
    Task<PackLoadResult> LoadPackAsync(string path);
}
