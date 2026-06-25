using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mTiles.Models;
using mTiles.Services;

namespace mTiles.ViewModels;

public partial class WorkspacesPanelViewModel : ObservableObject, IDisposable
{
    private readonly WorkspaceService _workspaceService;
    private readonly SettingsService? _settingsService;
    private readonly DispatcherTimer _branchTimer;

    public ObservableCollection<WorkspaceItemViewModel> Workspaces { get; } = [];

    public ObservableCollection<WorkspaceItemViewModel> FilteredWorkspaces { get; } = [];

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _showFilter;

    [ObservableProperty]
    private WorkspaceItemViewModel? _selectedWorkspace;

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    partial void OnSelectedWorkspaceChanged(WorkspaceItemViewModel? oldValue, WorkspaceItemViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        ApplyFilter();
    }

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    public Func<Task<string?>>? FolderPicker { get; set; }
    public Func<string, Task<bool>>? ConfirmAction { get; set; }
    public Action? FocusWorkspaceRequested { get; set; }

    public WorkspacesPanelViewModel(WorkspaceService workspaceService, SettingsService? settingsService = null)
    {
        _workspaceService = workspaceService;
        _settingsService = settingsService;

        var s = settingsService?.Settings;
        _fontFamily = s?.FontFamily ?? AppDefaults.FontFamily;
        _fontSize = s?.FontSize ?? AppDefaults.FontSize;

        if (_settingsService != null)
            _settingsService.SettingsChanged += OnSettingsChanged;

        Workspaces.CollectionChanged += (_, _) =>
        {
            ShowFilter = Workspaces.Count > 3;
            if (!ShowFilter) FilterText = string.Empty;
            ApplyFilter();
        };

        foreach (var w in workspaceService.Workspaces.OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase))
            Workspaces.Add(new WorkspaceItemViewModel(w));

        _branchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _branchTimer.Tick += async (_, _) =>
        {
            try { await RefreshAllBranchesAsync(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("Branch refresh failed: {0}", ex.Message); }
        };
        _branchTimer.Start();

        _ = RefreshAllBranchesAsync();
    }

    private async Task RefreshAllBranchesAsync()
    {
        var gitPath = GitService.ResolveGitPath(_settingsService?.Settings.GitPath);
        foreach (var item in Workspaces.ToList())
        {
            try
            {
                var branch = await GitService.GetBranchNameAsync(item.DirectoryPath, gitPath);
                if (branch != item.BranchName)
                    item.BranchName = branch;
            }
            catch (Exception ex) { Trace.TraceWarning("Branch lookup failed for {0}: {1}", item.DirectoryPath, ex.Message); }
        }
    }

    private void OnSettingsChanged()
    {
        var s = _settingsService!.Settings;
        if (s.FontFamily != FontFamily)
            FontFamily = s.FontFamily;
        if (Math.Abs(s.FontSize - FontSize) > AppDefaults.FontSizeEpsilon)
            FontSize = s.FontSize;
    }

    [RelayCommand]
    private void SelectWorkspace(WorkspaceItemViewModel item)
    {
        SelectedWorkspace = item;
        FocusWorkspaceRequested?.Invoke();
    }

    [RelayCommand]
    private async Task AddWorkspaceAsync()
    {
        var path = FolderPicker != null ? await FolderPicker() : null;
        if (string.IsNullOrEmpty(path)) return;

        var workspace = _workspaceService.AddWorkspace(path);
        var item = new WorkspaceItemViewModel(workspace);
        var index = 0;
        while (index < Workspaces.Count && string.Compare(Workspaces[index].Name, item.Name, StringComparison.OrdinalIgnoreCase) < 0)
            index++;
        Workspaces.Insert(index, item);
        SelectedWorkspace = item;

        var gitPath = GitService.ResolveGitPath(_settingsService?.Settings.GitPath);
        try { item.BranchName = await GitService.GetBranchNameAsync(item.DirectoryPath, gitPath); }
        catch (Exception ex) { Trace.TraceWarning("Branch lookup failed: {0}", ex.Message); }
    }

    [RelayCommand]
    private void OpenInFileManager(WorkspaceItemViewModel item)
    {
        var path = item.DirectoryPath;
        if (!Directory.Exists(path)) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start(new ProcessStartInfo("open", path) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start(new ProcessStartInfo("xdg-open", path) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task RemoveWorkspaceAsync(WorkspaceItemViewModel item)
    {
        if (ConfirmAction != null)
        {
            var confirmed = await ConfirmAction($"Remove workspace \"{item.Name}\"?");
            if (!confirmed) return;
        }

        _workspaceService.RemoveWorkspace(item.Id);
        Workspaces.Remove(item);
        if (SelectedWorkspace == item)
            SelectedWorkspace = Workspaces.FirstOrDefault();
    }

    private void ApplyFilter()
    {
        var filter = FilterText.Trim();
        var tokens = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        FilteredWorkspaces.Clear();
        foreach (var w in Workspaces)
        {
            if (tokens.Length == 0 || w == SelectedWorkspace || MatchesAllTokens(w, tokens))
                FilteredWorkspaces.Add(w);
        }
    }

    private static bool MatchesAllTokens(WorkspaceItemViewModel w, string[] tokens)
    {
        var haystack = $"{w.Name} {w.DirectoryPath}";
        foreach (var token in tokens)
        {
            if (!haystack.Contains(token, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    public void Dispose()
    {
        _branchTimer.Stop();
        if (_settingsService != null)
            _settingsService.SettingsChanged -= OnSettingsChanged;
    }
}
