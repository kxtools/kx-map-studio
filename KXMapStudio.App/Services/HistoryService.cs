using CommunityToolkit.Mvvm.ComponentModel;

using KXMapStudio.App.Actions;

namespace KXMapStudio.App.Services
{
    public partial class HistoryService : ObservableObject
    {
        private readonly Stack<IAction> _undoStack = new();
        private readonly Stack<IAction> _redoStack = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanUndo))]
        private int _undoCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRedo))]
        private int _redoCount;

        public bool CanUndo => UndoCount > 0;
        public bool CanRedo => RedoCount > 0;

        public void Do(IAction action)
        {
            action.Execute();
            _undoStack.Push(action);
            _redoStack.Clear();
            UpdateCounts();
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            UpdateCounts();
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            var action = _redoStack.Pop();
            action.Execute();
            _undoStack.Push(action);
            UpdateCounts();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            UndoCount = _undoStack.Count;
            RedoCount = _redoStack.Count;
        }

        /// <summary>
        /// Gets the type of the last action on the undo stack without removing it.
        /// </summary>
        /// <returns>The type of the last action, or a default value if the stack is empty.</returns>
        public ActionType PeekLastActionType()
        {
            return _undoStack.Any() ? _undoStack.Peek().Type : ActionType.Other;
        }
    }
}
