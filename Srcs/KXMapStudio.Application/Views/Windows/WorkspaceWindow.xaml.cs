namespace KXMapStudio.Application.Views.Windows;

public sealed partial class WorkspaceWindow : Window
{
	public WorkspaceWindow(IWorkspaceWindowViewModel viewModel, IGlobalHotkeyService hotkeyService)
	{
		InitializeComponent();
		DataContext = viewModel;

		// Initialize hotkey service with this window's handle for global hotkey support
		hotkeyService.Initialize(this);
		hotkeyService.RegisterHotkeys();

		Closed += (_, _) => hotkeyService.Dispose();
	}
}