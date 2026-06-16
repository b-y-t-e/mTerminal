using System.Text.Json;
using MTerminal.Models;

namespace MTerminal.Services;

public sealed class PersistenceService
{
    private readonly string _workspacesDir;
    private Timer? _debounceTimer;

    public PersistenceService()
    {
        _workspacesDir = Path.Combine(SettingsService.GetAppDataDirectory(), "workspaces");
        Directory.CreateDirectory(_workspacesDir);
    }

    public WorkspaceState? LoadLayout(string workspaceId)
    {
        var filePath = GetFilePath(workspaceId);
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WorkspaceState>(json, JsonDefaults.Options);
        }
        catch
        {
            return null;
        }
    }

    public void SaveLayout(string workspaceId, PaneNode? rootPane)
    {
        var state = new WorkspaceState
        {
            WorkspaceId = workspaceId,
            RootPane = rootPane
        };
        var json = JsonSerializer.Serialize(state, JsonDefaults.Options);
        File.WriteAllText(GetFilePath(workspaceId), json);
    }

    public void DebouncedSaveLayout(string workspaceId, Func<PaneNode?> getRootPane)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            try { SaveLayout(workspaceId, getRootPane()); }
            catch { }
        }, null, 1000, Timeout.Infinite);
    }

    public void DeleteLayout(string workspaceId)
    {
        var filePath = GetFilePath(workspaceId);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private string GetFilePath(string workspaceId) =>
        Path.Combine(_workspacesDir, $"{workspaceId}.json");
}
