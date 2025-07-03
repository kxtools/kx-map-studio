using KXMapStudio.App.Actions;
using KXMapStudio.App.Services;
using KXMapStudio.App.State;
using KXMapStudio.Core;
using System.Collections.Specialized;
using System.Windows;

namespace KXMapStudio.App.ViewModels;

public partial class MainViewModel
{
    private void WireEvents()
    {
        PackState.ActiveDocumentMarkers.CollectionChanged += OnActiveDocumentMarkersChanged;
        PackState.PropertyChanged += OnPackStateChanged;
        PackState.SelectedMarkers.CollectionChanged += OnSelectedMarkersChanged;
        _historyService.PropertyChanged += OnHistoryChanged;
        MumbleService.PropertyChanged += OnMumbleServiceChanged;
    }

    private void OnPackStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IPackStateService.IsWorkspaceLoaded):
            case nameof(IPackStateService.HasUnsavedChanges):
            case nameof(IPackStateService.IsWorkspaceArchive):
                OnPropertyChanged(nameof(Title));
                CloseWorkspaceCommand.NotifyCanExecuteChanged();
                SaveDocumentCommand.NotifyCanExecuteChanged();
                SaveAsCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.ActiveDocumentPath):
                OnPropertyChanged(nameof(Title));
                UpdateMarkersInView();
                AddMarkerFromGameCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.SelectedCategory):
                UpdateMarkersInView();
                break;
        }
    }

    private void OnSelectedMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CopySelectedMarkerGuidCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersUpCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersDownCommand.NotifyCanExecuteChanged();
    }

    private void OnHistoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistoryService.CanUndo))
        {
            UndoCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(HistoryService.CanRedo))
        {
            RedoCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnMumbleServiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MumbleService.IsAvailable))
        {
            AddMarkerFromGameCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnActiveDocumentMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UpdateMarkersInView();
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<Marker>())
            {
                MarkersInView.Remove(item);
            }
        }

        if (e.NewItems != null)
        {
            if (e.NewStartingIndex > -1)
            {
                int index = e.NewStartingIndex;
                foreach (var item in e.NewItems.OfType<Marker>())
                {
                    MarkersInView.Insert(index++, item);
                }
            }
            else
            {
                foreach (var item in e.NewItems.OfType<Marker>())
                {
                    MarkersInView.Add(item);
                }
            }
        }
    }

    private void TryUndoLastAddMarker()
    {
        if (_historyService.CanUndo && _historyService.PeekLastActionType() == ActionType.AddMarker)
        {
            _historyService.Undo();
            _feedbackService.ShowMessage("Undid last marker addition via hotkey.");
        }
        else
        {
            _feedbackService.ShowMessage("Cannot undo: last action was not a marker addition.", actionContent: "OK");
        }
    }
}
