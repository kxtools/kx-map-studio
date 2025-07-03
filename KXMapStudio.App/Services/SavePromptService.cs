using System.Windows;
using KXMapStudio.App.State;

namespace KXMapStudio.App.Services
{
    public class SavePromptService : ISavePromptService
    {
        private readonly IDialogService _dialogService;

        public SavePromptService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public async Task<bool> PromptToSaveChanges(IPackStateService packState)
        {
            if (packState.WorkspacePack == null)
            {
                return true;
            }

            var unsavedPaths = packState.WorkspacePack.GetUnsavedDocumentPaths().ToList();
            if (!unsavedPaths.Any())
            {
                return true;
            }

            foreach (var path in unsavedPaths)
            {
                // Temporarily set active document to the one with unsaved changes for accurate prompt
                packState.ActiveDocumentPath = path;

                string message;
                MessageBoxResult result;
                if (packState.IsWorkspaceArchive)
                {
                    message = $"You have unsaved changes in '{path}' (from a read-only archive). Would you like to save a copy?";
                    result = MessageBox.Show(message, "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                }
                else
                {
                    message = $"You have unsaved changes in '{path}'. Would you like to save them?";
                    result = MessageBox.Show(message, "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                }

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        bool saveSuccess = false;
                        if (packState.IsWorkspaceArchive || (packState.ActiveDocumentPath != null && packState.ActiveDocumentPath.StartsWith("Untitled-")))
                        {
                            await packState.SaveActiveDocumentAsAsync();
                            saveSuccess = !(packState.WorkspacePack?.HasUnsavedChangesFor(path) ?? true);
                        }
                        else
                        {
                            await packState.SaveActiveDocumentAsync();
                            saveSuccess = !(packState.WorkspacePack?.HasUnsavedChangesFor(path) ?? true);
                        }

                        if (!saveSuccess)
                        {
                            return false;
                        }
                        break;
                    case MessageBoxResult.No:
                        if (packState.WorkspacePack != null)
                        {
                            packState.RevertDocumentChanges(path);
                        }
                        continue;
                    case MessageBoxResult.Cancel:
                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
