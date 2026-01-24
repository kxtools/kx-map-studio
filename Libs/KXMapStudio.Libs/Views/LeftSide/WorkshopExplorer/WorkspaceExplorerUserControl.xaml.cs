namespace KXMapStudio.Libs.Views.LeftSide.WorkshopExplorer;

/// <summary>
///     Interaction logic for WorkspaceExplorerUserControl.xaml
/// </summary>
/// <remarks>
///     This code-behind contains minimal UI event forwarding logic.
///     All presentation logic resides in <see cref="IWorkshopExplorerViewModel" />.
/// </remarks>
public sealed partial class WorkspaceExplorerUserControl : UserControl
{
	public WorkspaceExplorerUserControl()
	{
		InitializeComponent();
	}

	/// <summary>
	///     Forwards TreeView selection changed event to the ViewModel command.
	/// </summary>
	private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		if (DataContext is IWorkshopExplorerViewModel vm)
			vm.SelectNodeCommand.Execute(e);
	}

	/// <summary>
	///     Configures the context menu dynamically when it opens for file nodes.
	/// </summary>
	/// <remarks>
	///     This approach avoids complex XAML binding issues with ContextMenu by setting the
	///     DeleteFileCommand directly from the ViewModel when the menu opens.
	/// </remarks>
	private void TreeView_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
	{
		if (DataContext is not WorkshopExplorerViewModel viewModel)
			return;

		if (e.OriginalSource is not DependencyObject source)
			return;

		var treeViewItem = FindParent<TreeViewItem>(source);
		if (treeViewItem?.DataContext is not WorkshopExplorerNodeModel node)
			return;

		// Prevent context menu for directories
		if (node.IsDirectory)
		{
			e.Handled = true;
			return;
		}

		// Wire up the delete command
		var contextMenu = treeViewItem.ContextMenu;
		if (contextMenu == null || contextMenu.Items.Count == 0)
			return;

		MenuItem? deleteMenuItem = null;
		foreach (var item in contextMenu.Items)
			if (item is MenuItem menuItem &&
			    (menuItem.Name == "DeleteMenuItem" || Equals(menuItem.Tag, "Delete")))
			{
				deleteMenuItem = menuItem;
				break;
			}

		// Fallback: if no specifically tagged delete item is found, use the first MenuItem (if any)
		if (deleteMenuItem == null)
			foreach (var item in contextMenu.Items)
				if (item is MenuItem menuItem)
				{
					deleteMenuItem = menuItem;
					break;
				}

		if (deleteMenuItem != null)
			deleteMenuItem.Command = viewModel.DeleteFileCommand;
	}

	/// <summary>
	///     Finds the first parent of type <typeparamref name="T" /> in the visual tree.
	/// </summary>
	/// <typeparam name="T">The type of parent to find.</typeparam>
	/// <param name="child">The starting child element.</param>
	/// <returns>The parent element of type <typeparamref name="T" />, or <see langword="null" /> if not found.</returns>
	private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		var parent = VisualTreeHelper.GetParent(child);

		while (parent != null)
		{
			if (parent is T typedParent)
				return typedParent;

			parent = VisualTreeHelper.GetParent(parent);
		}

		return null;
	}
}