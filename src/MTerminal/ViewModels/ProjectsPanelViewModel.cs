using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class ProjectsPanelViewModel : ObservableObject
{
    private readonly ProjectService _projectService;

    public ObservableCollection<Project> Projects { get; } = [];

    [ObservableProperty]
    private Project? _selectedProject;

    public Func<Task<string?>>? FolderPicker { get; set; }

    public ProjectsPanelViewModel(ProjectService projectService)
    {
        _projectService = projectService;
        foreach (var p in projectService.Projects)
            Projects.Add(p);
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        var path = FolderPicker != null ? await FolderPicker() : null;
        if (string.IsNullOrEmpty(path)) return;

        var project = _projectService.AddProject(path);
        Projects.Add(project);
        SelectedProject = project;
    }

    [RelayCommand]
    private void RemoveProject(Project project)
    {
        _projectService.RemoveProject(project.Id);
        Projects.Remove(project);
        if (SelectedProject == project)
            SelectedProject = Projects.FirstOrDefault();
    }

    [RelayCommand]
    private void RenameProject(Project project)
    {
        _projectService.Save();
    }
}
