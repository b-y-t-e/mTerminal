using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mTiles.Models;
using mTiles.Services;
using mTiles.Services.Database;

namespace mTiles.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly PersistenceService _persistenceService;
    private readonly SettingsService _settingsService;
    private readonly DatabaseServiceManager? _dbManager;
    private readonly UpdateService _updateService;
    private readonly Dictionary<string, WorkspaceViewModel> _workspaceCache = new();

    [ObservableProperty]
    private WorkspacesPanelViewModel _workspacesPanel;

    [ObservableProperty]
    private WorkspaceViewModel? _currentWorkspace;

    [ObservableProperty]
    private SettingsViewModel _settings;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateVersion = "";

    public MainWindowViewModel(WorkspaceService workspaceService, PersistenceService persistenceService,
        SettingsService settingsService, DatabaseServiceManager? dbManager = null)
    {
        _persistenceService = persistenceService;
        _settingsService = settingsService;
        _dbManager = dbManager;
        _updateService = new UpdateService();
        _workspacesPanel = new WorkspacesPanelViewModel(workspaceService, settingsService);
        _settings = new SettingsViewModel(settingsService, dbManager);

        _updateService.UpdateAvailable += () =>
        {
            IsUpdateAvailable = true;
            UpdateVersion = _updateService.NewVersion ?? "";
        };
        _updateService.StartPeriodicCheck();

        _workspacesPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspacesPanelViewModel.SelectedWorkspace))
                SwitchToWorkspace(_workspacesPanel.SelectedWorkspace?.Workspace);
        };
        _workspacesPanel.Workspaces.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                foreach (var id in _workspaceCache.Keys.ToList())
                    OnWorkspaceRemoved(id);
            }
            else if (e.OldItems != null)
            {
                foreach (WorkspaceItemViewModel item in e.OldItems)
                    OnWorkspaceRemoved(item.Id);
            }
        };

        if (_workspacesPanel.Workspaces.Count > 0)
        {
            var lastId = _settingsService.Settings.LastWorkspaceId;
            var target = _workspacesPanel.Workspaces.FirstOrDefault(w => w.Id == lastId)
                         ?? _workspacesPanel.Workspaces[0];
            _workspacesPanel.SelectedWorkspace = target;
        }
    }

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    public Func<string, Task<bool>>? ConfirmAction { get; set; }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        if (!_updateService.HasUpdate) return;
        var confirm = ConfirmAction;
        if (confirm != null)
        {
            var accepted = await confirm($"Version {_updateService.NewVersion} is ready. Restart now to update?");
            if (!accepted) return;
        }
        _updateService.ApplyUpdate();
    }

    public event Action<string>? WorkspaceRemoved;

    public void DisposeAll()
    {
        _updateService.Dispose();
        foreach (var vm in _workspaceCache.Values)
            vm.Dispose();
        _workspaceCache.Clear();
        _workspacesPanel.Dispose();
    }

    private void OnWorkspaceRemoved(string workspaceId)
    {
        if (!_workspaceCache.Remove(workspaceId, out var vm)) return;
        vm.Dispose();
        WorkspaceRemoved?.Invoke(workspaceId);
    }

    private void SwitchToWorkspace(Workspace? workspace)
    {
        if (workspace == null)
        {
            CurrentWorkspace = null;
            return;
        }

        if (!_workspaceCache.TryGetValue(workspace.Id, out var vm))
        {
            vm = new WorkspaceViewModel(workspace, _persistenceService, _settingsService, _dbManager);
            _workspaceCache[workspace.Id] = vm;
        }

        CurrentWorkspace = vm;
        vm.ActivateLastTile();
        _settingsService.Settings.LastWorkspaceId = workspace.Id;
        _settingsService.DebouncedSave();
    }
}
