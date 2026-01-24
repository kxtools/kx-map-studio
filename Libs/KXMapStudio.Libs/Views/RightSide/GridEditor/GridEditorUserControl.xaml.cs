namespace KXMapStudio.Libs.Views.RightSide.GridEditor;

/// <summary>
///     Interaction logic for GridEditorUserControl.xaml
/// </summary>
/// <remarks>
///     <para>
///         This code-behind contains UI-specific event handling for:
///         <list type="bullet">
///             <item>Row selection and focus management when rows are added</item>
///             <item>Delete key handling for multi-row deletion</item>
///             <item>DataGrid ScrollIntoView and EditMode coordination</item>
///         </list>
///     </para>
///     <para>
///         All business logic resides in <see cref="IGridEditorViewModel" />.
///         This code-behind only handles WPF-specific UI coordination.
///     </para>
/// </remarks>
public sealed partial class GridEditorUserControl : UserControl
{
	public GridEditorUserControl()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	/// <summary>
	///     Handles DataContext changes to subscribe/unsubscribe from ViewModel events.
	/// </summary>
	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		// Unsubscribe from old ViewModel
		if (e.OldValue is IGridEditorViewModel oldVm)
			oldVm.RowAdded -= OnRowAdded;

		// Subscribe to new ViewModel
		if (e.NewValue is IGridEditorViewModel newVm)
			newVm.RowAdded += OnRowAdded;
	}

	/// <summary>
	///     Handles RowAdded event from ViewModel to focus and select the newly added row.
	/// </summary>
	/// <param name="sender">The event source (ViewModel).</param>
	/// <param name="addedRow">The newly added row to focus.</param>
	private void OnRowAdded(object? sender, GridEditorRowViewModel addedRow)
	{
		// Use Dispatcher to ensure we're on UI thread and DataGrid is updated
		Dispatcher.BeginInvoke(new Action(() =>
		{
			// Select the newly added row
			MarkersDataGrid.SelectedItem = addedRow;

			// Scroll to make it visible
			MarkersDataGrid.ScrollIntoView(addedRow);

			// Set focus to the DataGrid and begin edit on Name column
			MarkersDataGrid.Focus();

			// Wait for the visual tree to update, then focus the Name cell
			MarkersDataGrid.UpdateLayout();

			var row = MarkersDataGrid.ItemContainerGenerator.ContainerFromItem(addedRow) as DataGridRow;
			if (row is not null)
				row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		}), DispatcherPriority.Background);
	}

	/// <summary>
	///     Handles Delete key press to delete selected rows via ViewModel command.
	/// </summary>
	/// <param name="sender">The DataGrid source.</param>
	/// <param name="e">The key event arguments.</param>
	private void MarkersDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Delete && DataContext is IGridEditorViewModel vm)
		{
			var selectedRows = MarkersDataGrid.SelectedItems.Cast<GridEditorRowViewModel>().ToList();
			foreach (var row in selectedRows)
				vm.DeleteRowCommand.Execute(row);
			e.Handled = true;
		}
	}
}