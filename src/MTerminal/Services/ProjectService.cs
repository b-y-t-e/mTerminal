using System.Text.Json;
using MTerminal.Models;

namespace MTerminal.Services;

public sealed class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;
    private List<Project> _projects = [];

    public IReadOnlyList<Project> Projects => _projects;

    public ProjectService()
    {
        var appDir = SettingsService.GetAppDataDirectory();
        Directory.CreateDirectory(appDir);
        _filePath = Path.Combine(appDir, "projects.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            _projects = JsonSerializer.Deserialize<List<Project>>(json, JsonOptions) ?? [];
        }
        catch
        {
            _projects = [];
        }
    }

    public Project AddProject(string directoryPath, string? name = null)
    {
        var project = new Project
        {
            Name = name ?? Path.GetFileName(directoryPath) ?? directoryPath,
            DirectoryPath = directoryPath
        };
        _projects.Add(project);
        Save();
        return project;
    }

    public void RemoveProject(string projectId)
    {
        _projects.RemoveAll(p => p.Id == projectId);
        Save();
    }

    public void RenameProject(string projectId, string newName)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        if (project != null)
        {
            project.Name = newName;
            Save();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_projects, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
