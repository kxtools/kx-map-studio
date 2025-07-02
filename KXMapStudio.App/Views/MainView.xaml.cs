using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using KXMapStudio.App.ViewModels;

using MaterialDesignThemes.Wpf;

namespace KXMapStudio.App.Views
{
    public partial class MainView : Window
    {
        public MainView(MainViewModel viewModel, SnackbarMessageQueue snackbarMessageQueue)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.Closing += MainView_Closing;

            MainSnackbar.MessageQueue = snackbarMessageQueue;
        }

        private async void MainView_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            if (!vm.PackState.HasUnsavedChanges)
            {
                return; // No unsaved changes, let the window close normally.
            }

            // 1. Immediately cancel the original close event to give our async code time to run.
            e.Cancel = true;

            // 2. Ask the user what to do with their unsaved changes.
            bool canProceed = await vm.PackState.CheckAndPromptToSaveChanges();

            // 3. If the user action succeeded (saved or discarded), shut down the application.
            if (canProceed)
            {
                // This is the key change. Instead of trying to close the window again,
                // we tell the entire application to shut down. This is safe and avoids the exception.
                Application.Current.Shutdown();
            }
            // If canProceed is false, the user clicked "Cancel", and we do nothing,
            // leaving the window open as intended.
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid)
            {
                return;
            }

            var dependencyObject = (DependencyObject)e.OriginalSource;
            while (dependencyObject != null && dependencyObject is not DataGridCell)
            {
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }
            if (dependencyObject is not DataGridCell cell || cell.IsEditing)
            {
                return;
            }

            if (cell.Column.IsReadOnly)
            {
                return;
            }

            if (cell.IsFocused && grid.SelectedItem == cell.DataContext)
            {
                grid.BeginEdit();
                e.Handled = true;
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is DataGrid grid)
                {
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                    e.Handled = true;
                }
            }
        }

        private async void WorkspaceFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // We only care about user-initiated selections.
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            var listBox = (ListBox)sender;
            var newPath = (string)e.AddedItems[0]!;

            // Get the source of truth for the current path from the ViewModel,
            // as the binding is now OneWay.
            var currentPath = vm.PackState.ActiveDocumentPath;

            // If the user simply re-clicked the already active item, do nothing.
            if (Equals(currentPath, newPath))
            {
                return;
            }

            // Ask the ViewModel for permission to change the document.
            bool canProceed = await vm.RequestChangeDocumentAsync();

            if (canProceed)
            {
                // Permission granted. Officially update the ViewModel's state.
                vm.PackState.ActiveDocumentPath = newPath;
            }
            else
            {
                // Permission denied (user clicked "Cancel").
                // Revert the ListBox's visual selection back to the original item.
                // To prevent this from re-triggering this event handler, we unhook it.
                listBox.SelectionChanged -= WorkspaceFilesListBox_SelectionChanged;
                listBox.SelectedItem = currentPath; // Revert to the old path from the ViewModel.
                listBox.SelectionChanged += WorkspaceFilesListBox_SelectionChanged;
            }
        }
    }
}
