namespace MTerminal.Models;

public sealed class TerminalTheme
{
    public string Name { get; init; } = string.Empty;
    public string Foreground { get; init; } = "#cccccc";
    public string Background { get; init; } = "#1a1a2e";
    public string Cursor { get; init; } = "#ffffff";
    public string Selection { get; init; } = "#264f78";
    public string Black { get; init; } = "#000000";
    public string Red { get; init; } = "#cd3131";
    public string Green { get; init; } = "#0dbc79";
    public string Yellow { get; init; } = "#e5e510";
    public string Blue { get; init; } = "#2472c8";
    public string Magenta { get; init; } = "#bc3fbc";
    public string Cyan { get; init; } = "#11a8cd";
    public string White { get; init; } = "#e5e5e5";
    public string BrightBlack { get; init; } = "#666666";
    public string BrightRed { get; init; } = "#f14c4c";
    public string BrightGreen { get; init; } = "#23d18b";
    public string BrightYellow { get; init; } = "#f5f543";
    public string BrightBlue { get; init; } = "#3b8eea";
    public string BrightMagenta { get; init; } = "#d670d6";
    public string BrightCyan { get; init; } = "#29b8db";
    public string BrightWhite { get; init; } = "#ffffff";

    public static IReadOnlyList<TerminalTheme> BuiltIn { get; } =
    [
        new()
        {
            Name = "Default Dark",
            Foreground = "#cccccc", Background = "#1e1e2e", Cursor = "#ffffff", Selection = "#264f78",
            Black = "#000000", Red = "#cd3131", Green = "#0dbc79", Yellow = "#e5e510",
            Blue = "#2472c8", Magenta = "#bc3fbc", Cyan = "#11a8cd", White = "#e5e5e5",
            BrightBlack = "#666666", BrightRed = "#f14c4c", BrightGreen = "#23d18b", BrightYellow = "#f5f543",
            BrightBlue = "#3b8eea", BrightMagenta = "#d670d6", BrightCyan = "#29b8db", BrightWhite = "#ffffff"
        },
        new()
        {
            Name = "Dracula",
            Foreground = "#f8f8f2", Background = "#282a36", Cursor = "#f8f8f2", Selection = "#44475a",
            Black = "#21222c", Red = "#ff5555", Green = "#50fa7b", Yellow = "#f1fa8c",
            Blue = "#bd93f9", Magenta = "#ff79c6", Cyan = "#8be9fd", White = "#f8f8f2",
            BrightBlack = "#6272a4", BrightRed = "#ff6e6e", BrightGreen = "#69ff94", BrightYellow = "#ffffa5",
            BrightBlue = "#d6acff", BrightMagenta = "#ff92df", BrightCyan = "#a4ffff", BrightWhite = "#ffffff"
        },
        new()
        {
            Name = "Nord",
            Foreground = "#d8dee9", Background = "#2e3440", Cursor = "#d8dee9", Selection = "#434c5e",
            Black = "#3b4252", Red = "#bf616a", Green = "#a3be8c", Yellow = "#ebcb8b",
            Blue = "#81a1c1", Magenta = "#b48ead", Cyan = "#88c0d0", White = "#e5e9f0",
            BrightBlack = "#4c566a", BrightRed = "#bf616a", BrightGreen = "#a3be8c", BrightYellow = "#ebcb8b",
            BrightBlue = "#81a1c1", BrightMagenta = "#b48ead", BrightCyan = "#8fbcbb", BrightWhite = "#eceff4"
        },
        new()
        {
            Name = "Monokai",
            Foreground = "#f8f8f2", Background = "#272822", Cursor = "#f8f8f2", Selection = "#49483e",
            Black = "#272822", Red = "#f92672", Green = "#a6e22e", Yellow = "#f4bf75",
            Blue = "#66d9ef", Magenta = "#ae81ff", Cyan = "#a1efe4", White = "#f8f8f2",
            BrightBlack = "#75715e", BrightRed = "#f92672", BrightGreen = "#a6e22e", BrightYellow = "#f4bf75",
            BrightBlue = "#66d9ef", BrightMagenta = "#ae81ff", BrightCyan = "#a1efe4", BrightWhite = "#f9f8f5"
        },
        new()
        {
            Name = "Solarized Dark",
            Foreground = "#839496", Background = "#002b36", Cursor = "#839496", Selection = "#073642",
            Black = "#073642", Red = "#dc322f", Green = "#859900", Yellow = "#b58900",
            Blue = "#268bd2", Magenta = "#d33682", Cyan = "#2aa198", White = "#eee8d5",
            BrightBlack = "#586e75", BrightRed = "#cb4b16", BrightGreen = "#586e75", BrightYellow = "#657b83",
            BrightBlue = "#839496", BrightMagenta = "#6c71c4", BrightCyan = "#93a1a1", BrightWhite = "#fdf6e3"
        },
        new()
        {
            Name = "Catppuccin Mocha",
            Foreground = "#cdd6f4", Background = "#1e1e2e", Cursor = "#f5e0dc", Selection = "#45475a",
            Black = "#45475a", Red = "#f38ba8", Green = "#a6e3a1", Yellow = "#f9e2af",
            Blue = "#89b4fa", Magenta = "#f5c2e7", Cyan = "#94e2d5", White = "#bac2de",
            BrightBlack = "#585b70", BrightRed = "#f38ba8", BrightGreen = "#a6e3a1", BrightYellow = "#f9e2af",
            BrightBlue = "#89b4fa", BrightMagenta = "#f5c2e7", BrightCyan = "#94e2d5", BrightWhite = "#a6adc8"
        }
    ];

    public static TerminalTheme GetByName(string? name) =>
        BuiltIn.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? BuiltIn[0];
}
