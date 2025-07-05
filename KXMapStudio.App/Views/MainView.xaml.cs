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
        private bool _isClosingHandled = false;

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
            if (_isClosingHandled)
            {
                e.Cancel = false; // Allow the window to close if already handled
                return;
            }

            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            if (!vm.PackState.HasUnsavedChanges)
            {
                return; // No unsaved changes, let the window close normally.
            }

            e.Cancel = true; // Prevent immediate close

            bool canProceed = await vm.PackState.CheckAndPromptToSaveChanges();

            if (canProceed)
            {
                _isClosingHandled = true; // Set the flag before initiating shutdown
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
            // We only care about the specific scenario where ONE new marker is added to the selection.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems?.Count == 1)
            {
                var newItem = e.NewItems[0];
                if (newItem != null)
                {
                    // Use the dispatcher to ensure the UI has finished generating the new row
                    // before we try to interact with it.
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // STEP 1: Scroll the new item into view.
                        // NOTE: We don't set SelectedItem here directly, as it interferes with multi-selection.
                        // The DataGrid's binding handles the selection state.
                        MarkersDataGrid.ScrollIntoView(newItem);

                        // STEP 3: Find the actual DataGridRow and give it keyboard focus.
                        // This prevents the "ghost" selection of the previous item.
                        var row = (DataGridRow)MarkersDataGrid.ItemContainerGenerator.ContainerFromItem(newItem);
                        if (row != null)
                        {
                            row.Focus();
                        }

                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
            }
        }

        private void SelectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MarkersDataGrid.SelectAll();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            var scrollViewer = GetScrollViewer(MarkersDataGrid);
            var offset = scrollViewer?.VerticalOffset ?? 0;

            var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
            vm.DeleteMarkers(selectedMarkers);

            Dispatcher.BeginInvoke(new Action(() => scrollViewer?.ScrollToVerticalOffset(offset)), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void MoveUpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            var scrollViewer = GetScrollViewer(MarkersDataGrid);
            var offset = scrollViewer?.VerticalOffset ?? 0;

            var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
            vm.MoveMarkersUp(selectedMarkers);

            Dispatcher.BeginInvoke(new Action(() => scrollViewer?.ScrollToVerticalOffset(offset)), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void MoveDownMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            var scrollViewer = GetScrollViewer(MarkersDataGrid);
            var offset = scrollViewer?.VerticalOffset ?? 0;

            var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
            vm.MoveMarkersDown(selectedMarkers);

            Dispatcher.BeginInvoke(new Action(() => scrollViewer?.ScrollToVerticalOffset(offset)), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void CopyGuidMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            var selectedMarkers = MarkersDataGrid.SelectedItems.OfType<Core.Marker>().ToList();
            vm.CopySelectedMarkerGuid(selectedMarkers);
        }

        private void InsertNewMarkerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            var scrollViewer = GetScrollViewer(MarkersDataGrid);
            var offset = scrollViewer?.VerticalOffset ?? 0;

            var selectedMarker = MarkersDataGrid.SelectedItem as Core.Marker;
            vm.InsertNewMarkerCommand.Execute(selectedMarker);

            Dispatcher.BeginInvoke(new Action(() => scrollViewer?.ScrollToVerticalOffset(offset)), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        public static ScrollViewer? GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer) return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
