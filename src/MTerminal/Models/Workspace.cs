namespace MTerminal.Models;

public sealed class Workspace
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
}
