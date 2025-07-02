using KXMapStudio.App.Actions;

namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Represents a reversible user action.
    /// </summary>
    public interface IAction
    {
        void Execute();
        void Undo();

        ActionType Type { get; }
    }
}
