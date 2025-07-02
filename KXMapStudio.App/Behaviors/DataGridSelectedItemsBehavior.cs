using System.Collections;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

namespace KXMapStudio.App.Behaviors;

/// <summary>
/// Enables two-way binding for the DataGrid.SelectedItems property.
/// </summary>
public class DataGridSelectedItemsBehavior : Behavior<DataGrid>
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(IList),
            typeof(DataGridSelectedItemsBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.SelectionChanged += OnDataGridSelectionChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.SelectionChanged -= OnDataGridSelectionChanged;
        }
    }

    /// <summary>
    /// Synchronizes changes from the DataGrid's selection to the bound collection.
    /// </summary>
    private void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedItems == null)
        {
            return;
        }

        foreach (var item in e.RemovedItems)
        {
            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
            }
        }

        foreach (var item in e.AddedItems)
        {
            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
            }
        }
    }

    /// <summary>
    /// Synchronizes changes from the bound collection to the DataGrid's selection.
    /// </summary>
    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGridSelectedItemsBehavior behavior || behavior.AssociatedObject == null)
        {
            return;
        }

        var dataGrid = behavior.AssociatedObject;

        dataGrid.SelectionChanged -= behavior.OnDataGridSelectionChanged;

        dataGrid.SelectedItems.Clear();
        if (e.NewValue is IList newSelection)
        {
            foreach (var item in newSelection)
            {
                dataGrid.SelectedItems.Add(item);
            }
        }

        dataGrid.SelectionChanged += behavior.OnDataGridSelectionChanged;
    }
}
