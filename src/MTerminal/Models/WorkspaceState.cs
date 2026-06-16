namespace MTerminal.Models;

public sealed class WorkspaceState
{
    public string WorkspaceId { get; set; } = string.Empty;
    public PaneNode? RootPane { get; set; }
}
