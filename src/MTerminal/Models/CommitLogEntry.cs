namespace MTerminal.Models;

public sealed class CommitLogEntry
{
    public required string Hash { get; init; }
    public required string Message { get; init; }
    public List<string> Tags { get; init; } = [];
    public bool IsPushed { get; init; } = true;

    public string TagsDisplay => string.Join("  ", Tags);
    public bool HasTags => Tags.Count > 0;
    public bool IsLocal => !IsPushed;
    public string Display => $"{Hash[..Math.Min(7, Hash.Length)]}  {Message}";
}
