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

            viewModel.PackState.SelectedMarkers.CollectionChanged += SelectedMarkers_CollectionChanged;
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

            e.Cancel = true;

            bool canProceed = await vm.PackState.CheckAndPromptToSaveChanges();

            if (canProceed)
            {
                Application.Current.Shutdown();
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

        private async void WorkspaceFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            var currentPath = vm.PackState.ActiveDocumentPath;

            if (Equals(currentPath, newPath))
            {
                return;
            }

            bool canProceed = await vm.RequestChangeDocumentAsync();

            if (canProceed)
            {
                vm.PackState.ActiveDocumentPath = newPath;
            }
            else
            {
                listBox.SelectionChanged -= WorkspaceFilesListBox_SelectionChanged;
                listBox.SelectedItem = currentPath;
                listBox.SelectionChanged += WorkspaceFilesListBox_SelectionChanged;
            }
        }

        private void SelectedMarkers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems?.Count == 1)
            {
                var newItem = e.NewItems[0];
                if (newItem != null)
                {
                    MarkersDataGrid.ScrollIntoView(newItem);
                }
            }
        }

        private void SelectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MarkersDataGrid.SelectAll();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();

                vm.DeleteMarkers(selectedMarkers);
            }
        }

        private void InsertMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Get the selection DIRECTLY from the DataGrid UI element at the moment of the click.
                var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();

                // Call a new method on the ViewModel, passing the actual selection.
                vm.InsertNewMarker(selectedMarkers);
            }
        }

        private void MoveUpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
                vm.MoveMarkersUp(selectedMarkers);
            }
        }

        private void MoveDownMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
                vm.MoveMarkersDown(selectedMarkers);
            }
        }

        private void CopyGuidMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
                vm.CopySelectedMarkerGuid(selectedMarkers);
            }
        }
    }
}
