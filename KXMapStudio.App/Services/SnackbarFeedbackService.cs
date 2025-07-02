using MaterialDesignThemes.Wpf;

namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Provides user feedback using the Material Design Snackbar.
    /// </summary>
    public class SnackbarFeedbackService : IFeedbackService
    {
        private readonly ISnackbarMessageQueue _messageQueue;

        public SnackbarFeedbackService(ISnackbarMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        /// <summary>
        /// Shows a message in the snackbar.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="actionContent">Optional text for an action button.</param>
        /// <param name="actionHandler">Optional handler for the action button.</param>
        public void ShowMessage(string message, string? actionContent = null, Action? actionHandler = null)
        {
            if (actionContent != null)
            {
                var handler = actionHandler ?? (() => { });
                _messageQueue.Enqueue(message, actionContent, handler);
            }
            else
            {
                _messageQueue.Enqueue(message);
            }
        }
    }
}
