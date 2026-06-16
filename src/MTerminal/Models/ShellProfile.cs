namespace MTerminal.Models;

public sealed class ShellProfile
{
    public string Name { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public string[] Args { get; init; } = [];
}
