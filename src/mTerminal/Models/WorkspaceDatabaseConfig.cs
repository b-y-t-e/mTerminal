namespace mTerminal.Models;

public sealed class WorkspaceDatabaseConfig
{
    public string DatabaseKey { get; set; } = "";
    public bool AllowModifications { get; set; }
}
