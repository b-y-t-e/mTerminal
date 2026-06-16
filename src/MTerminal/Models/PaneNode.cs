namespace MTerminal.Models;

public sealed class PaneNode
{
    public bool IsLeaf { get; set; }

    public PaneContentType ContentType { get; set; }
    public string? EditorFilePath { get; set; }

    public string SplitOrientation { get; set; } = "Vertical";
    public double SplitRatio { get; set; } = 0.5;
    public PaneNode? First { get; set; }
    public PaneNode? Second { get; set; }
}
