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
    private ProjectsPanelViewModel _projectsPanel;

    [ObservableProperty]
    private WorkspaceViewModel? _currentWorkspace;

    [ObservableProperty]
    private SettingsViewModel _settings;

    [ObservableProperty]
    private bool _isSettingsOpen;

    public MainWindowViewModel(ProjectService projectService, PersistenceService persistenceService,
        SettingsService settingsService)
    {
        _persistenceService = persistenceService;
        _settingsService = settingsService;
        _projectsPanel = new ProjectsPanelViewModel(projectService);
        _settings = new SettingsViewModel(settingsService);

        _projectsPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProjectsPanelViewModel.SelectedProject))
                SwitchToProject(_projectsPanel.SelectedProject);
        };

        if (_projectsPanel.Projects.Count > 0)
        {
            _projectsPanel.SelectedProject = _projectsPanel.Projects[0];
        }
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    private void SwitchToProject(Project? project)
    {
        if (project == null)
        {
            CurrentWorkspace = null;
            return;
        }

        if (!_workspaceCache.TryGetValue(project.Id, out var workspace))
        {
            workspace = new WorkspaceViewModel(project, _persistenceService, _settingsService);
            _workspaceCache[project.Id] = workspace;
        }

        CurrentWorkspace = workspace;
    }
}
