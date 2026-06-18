namespace MTerminal.Models;

public sealed class UserAiTool
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string BinaryName { get; set; } = "";
    public string VersionArgs { get; set; } = "--version";
    public string CustomPath { get; set; } = "";
}
