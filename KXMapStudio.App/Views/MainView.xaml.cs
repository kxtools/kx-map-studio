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

            if (vm.PackState.HasUnsavedChanges)
            {
                e.Cancel = true;

                bool canProceed = await vm.PackState.CheckAndPromptToSaveChanges();

                if (canProceed)
                {
                    // It will enter this event handler again, but HasUnsavedChanges will be false.
                    this.Closing -= MainView_Closing;
                    this.Close();
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
