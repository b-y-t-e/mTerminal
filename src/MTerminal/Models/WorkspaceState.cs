namespace MTerminal.Models;

public sealed class WorkspaceState
{
    public string ProjectId { get; set; } = string.Empty;
    public PaneNode? RootPane { get; set; }
}
