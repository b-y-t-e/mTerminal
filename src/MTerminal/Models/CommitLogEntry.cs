namespace MTerminal.Models;

public sealed class CommitLogEntry
{
    public required string Hash { get; init; }
    public required string Message { get; init; }
    public string Display => $"{Hash[..Math.Min(7, Hash.Length)]}  {Message}";
}
