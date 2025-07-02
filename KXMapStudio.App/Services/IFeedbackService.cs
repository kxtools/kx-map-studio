namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Defines a service for showing non-blocking user feedback.
    /// </summary>
    public interface IFeedbackService
    {
        /// <summary>
        /// Shows a message to the user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="actionContent">Optional text for an action button.</param>
        /// <param name="actionHandler">Optional action for the button.</param>
        void ShowMessage(string message, string? actionContent = null, Action? actionHandler = null);
    }
}
