using KXMapStudio.App.Actions;

namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Represents a reversible user action.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Executes the action on the data model.
        /// </summary>
        /// <returns>True if the action was successful, otherwise false.</returns>
        bool Execute();

        /// <summary>
        /// Reverts the action on the data model.
        /// </summary>
        /// <returns>True if the undo was successful, otherwise false.</returns>
        bool Undo();

        ActionType Type { get; }
    }
}
