using System.Text.Json;
using mTiles.Models;

namespace mTiles.Services;

public sealed class WorkspaceService
{
    private readonly string _filePath;
    private List<Workspace> _workspaces = [];

    public IReadOnlyList<Workspace> Workspaces => _workspaces;

    public WorkspaceService()
    {
        var appDir = AppPaths.GetAppDataDirectory();
        Directory.CreateDirectory(appDir);
        _filePath = AppPaths.GetWorkspacesFilePath();
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            _workspaces = JsonSerializer.Deserialize<List<Workspace>>(json, JsonDefaults.Options) ?? [];
        }
        catch
        {
            _workspaces = [];
        }

        var fixed_ = false;
        foreach (var w in _workspaces)
        {
            if (!string.IsNullOrEmpty(w.Name)) continue;
            w.Name = Path.GetFileName(w.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                     ?? w.DirectoryPath;
            fixed_ = true;
        }
        if (fixed_) Save();
    }

    public Workspace AddWorkspace(string directoryPath, string? name = null)
    {
        var workspace = new Workspace
        {
            Name = name ?? Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? directoryPath,
            DirectoryPath = directoryPath
        };
        _workspaces.Add(workspace);
        Save();
        return workspace;
    }

    public void RemoveWorkspace(string workspaceId)
    {
        _workspaces.RemoveAll(w => w.Id == workspaceId);
        Save();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_workspaces, JsonDefaults.Options);
        File.WriteAllText(_filePath, json);
    }
}
