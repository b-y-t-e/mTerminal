using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly PersistenceService _persistenceService;
    private readonly SettingsService _settingsService;
    private readonly Dictionary<string, WorkspaceViewModel> _workspaceCache = new();

    [ObservableProperty]
    private bool _isPanelOpen = true;

    [ObservableProperty]
    private WorkspacesPanelViewModel _workspacesPanel;

    [ObservableProperty]
    private WorkspaceViewModel? _currentWorkspace;

    [ObservableProperty]
    private SettingsViewModel _settings;

    [ObservableProperty]
    private bool _isSettingsOpen;

    public MainWindowViewModel(WorkspaceService workspaceService, PersistenceService persistenceService,
        SettingsService settingsService)
    {
        _persistenceService = persistenceService;
        _settingsService = settingsService;
        _workspacesPanel = new WorkspacesPanelViewModel(workspaceService);
        _settings = new SettingsViewModel(settingsService);

        _workspacesPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspacesPanelViewModel.SelectedWorkspace))
                SwitchToWorkspace(_workspacesPanel.SelectedWorkspace);
        };

        if (_workspacesPanel.Workspaces.Count > 0)
            _workspacesPanel.SelectedWorkspace = _workspacesPanel.Workspaces[0];
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    private void SwitchToWorkspace(Workspace? workspace)
    {
        if (workspace == null)
        {
            CurrentWorkspace = null;
            return;
        }

        if (!_workspaceCache.TryGetValue(workspace.Id, out var vm))
        {
            vm = new WorkspaceViewModel(workspace, _persistenceService, _settingsService);
            _workspaceCache[workspace.Id] = vm;
        }

        CurrentWorkspace = vm;
    }
}
