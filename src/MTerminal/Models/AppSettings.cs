namespace MTerminal.Models;

public sealed class AppSettings
{
    public string TerminalFontFamily { get; set; } = "Cascadia Mono, Consolas, monospace";
    public double TerminalFontSize { get; set; } = 14;
    public string EditorFontFamily { get; set; } = "Cascadia Mono, Consolas, monospace";
    public double EditorFontSize { get; set; } = 14;
    public string Theme { get; set; } = "Dark";
}
