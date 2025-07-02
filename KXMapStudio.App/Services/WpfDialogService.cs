using System.Windows;

namespace KXMapStudio.App.Services
{
    public class WpfDialogService : IDialogService
    {
        public void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
