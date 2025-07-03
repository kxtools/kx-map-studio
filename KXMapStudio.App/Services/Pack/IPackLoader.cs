using KXMapStudio.Core;
using System.Threading.Tasks;

namespace KXMapStudio.App.Services.Pack;

public interface IPackLoader
{
    Task<PackLoadResult> LoadPackAsync(string path);
}
