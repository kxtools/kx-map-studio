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

        private void MainView_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            if (vm.PackState.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Would you like to save before exiting?",
                    "Exit Application",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    vm.SaveDocumentCommand.Execute(null);

                    if (vm.PackState.HasUnsavedChanges)
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
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
    }
}
