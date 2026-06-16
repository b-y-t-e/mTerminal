using System.Text.Json;
using MTerminal.Models;

namespace MTerminal.Services;

public sealed class PersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _workspacesDir;
    private Timer? _debounceTimer;

    public PersistenceService()
    {
        _workspacesDir = Path.Combine(SettingsService.GetAppDataDirectory(), "workspaces");
        Directory.CreateDirectory(_workspacesDir);
    }

    public WorkspaceState? LoadWorkspace(string projectId)
    {
        var filePath = GetWorkspaceFilePath(projectId);
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WorkspaceState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void SaveWorkspace(string projectId, PaneNode? rootPane)
    {
        var state = new WorkspaceState
        {
            ProjectId = projectId,
            RootPane = rootPane
        };
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(GetWorkspaceFilePath(projectId), json);
    }

    public void DebouncedSaveWorkspace(string projectId, Func<PaneNode?> getRootPane)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            try
            {
                SaveWorkspace(projectId, getRootPane());
            }
            catch { }
        }, null, 1000, Timeout.Infinite);
    }

    public void DeleteWorkspace(string projectId)
    {
        var filePath = GetWorkspaceFilePath(projectId);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private string GetWorkspaceFilePath(string projectId) =>
        Path.Combine(_workspacesDir, $"{projectId}.json");
}
