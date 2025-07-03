using KXMapStudio.App.State;

namespace KXMapStudio.App.Services
{
    public interface ISavePromptService
    {
        Task<bool> PromptToSaveChanges(IPackStateService packState);
    }
}
