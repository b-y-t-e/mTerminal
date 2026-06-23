namespace mTiles.Models;

public sealed class TerminalTheme
{
    public string Name { get; init; } = string.Empty;
    public bool IsDark { get; init; } = true;
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
            Foreground = "#cdd6f4", Background = "#1e1e2e", Cursor = "#f5e0dc", Selection = "#585b70",
            Black = "#45475a", Red = "#f38ba8", Green = "#a6e3a1", Yellow = "#f9e2af",
            Blue = "#89b4fa", Magenta = "#f5c2e7", Cyan = "#94e2d5", White = "#bac2de",
            BrightBlack = "#585b70", BrightRed = "#f38ba8", BrightGreen = "#a6e3a1", BrightYellow = "#f9e2af",
            BrightBlue = "#89b4fa", BrightMagenta = "#f5c2e7", BrightCyan = "#94e2d5", BrightWhite = "#a6adc8"
        },
        new()
        {
            Name = "Catppuccin Macchiato",
            Foreground = "#cad3f5", Background = "#24273a", Cursor = "#f4dbd6", Selection = "#5b6078",
            Black = "#494d64", Red = "#ed8796", Green = "#a6da95", Yellow = "#eed49f",
            Blue = "#8aadf4", Magenta = "#f5bde6", Cyan = "#8bd5ca", White = "#b8c0e0",
            BrightBlack = "#5b6078", BrightRed = "#ed8796", BrightGreen = "#a6da95", BrightYellow = "#eed49f",
            BrightBlue = "#8aadf4", BrightMagenta = "#f5bde6", BrightCyan = "#8bd5ca", BrightWhite = "#a5adcb"
        },
        new()
        {
            Name = "Catppuccin Frappé",
            Foreground = "#c6d0f5", Background = "#303446", Cursor = "#f2d5cf", Selection = "#626880",
            Black = "#51576d", Red = "#e78284", Green = "#a6d189", Yellow = "#e5c890",
            Blue = "#8caaee", Magenta = "#f4b8e4", Cyan = "#81c8be", White = "#b5bfe2",
            BrightBlack = "#626880", BrightRed = "#e78284", BrightGreen = "#a6d189", BrightYellow = "#e5c890",
            BrightBlue = "#8caaee", BrightMagenta = "#f4b8e4", BrightCyan = "#81c8be", BrightWhite = "#a5adce"
        },
        new()
        {
            Name = "Tokyo Night",
            Foreground = "#a9b1d6", Background = "#1a1b26", Cursor = "#c0caf5", Selection = "#283457",
            Black = "#15161e", Red = "#f7768e", Green = "#9ece6a", Yellow = "#e0af68",
            Blue = "#7aa2f7", Magenta = "#bb9af7", Cyan = "#7dcfff", White = "#a9b1d6",
            BrightBlack = "#414868", BrightRed = "#f7768e", BrightGreen = "#9ece6a", BrightYellow = "#e0af68",
            BrightBlue = "#7aa2f7", BrightMagenta = "#bb9af7", BrightCyan = "#7dcfff", BrightWhite = "#c0caf5"
        },
        new()
        {
            Name = "Gruvbox Dark",
            Foreground = "#ebdbb2", Background = "#282828", Cursor = "#ebdbb2", Selection = "#504945",
            Black = "#282828", Red = "#cc241d", Green = "#98971a", Yellow = "#d79921",
            Blue = "#458588", Magenta = "#b16286", Cyan = "#689d6a", White = "#a89984",
            BrightBlack = "#928374", BrightRed = "#fb4934", BrightGreen = "#b8bb26", BrightYellow = "#fabd2f",
            BrightBlue = "#83a598", BrightMagenta = "#d3869b", BrightCyan = "#8ec07c", BrightWhite = "#fbf1c7"
        },
        new()
        {
            Name = "One Dark",
            Foreground = "#abb2bf", Background = "#21252b", Cursor = "#abb2bf", Selection = "#323843",
            Black = "#21252b", Red = "#e06c75", Green = "#98c379", Yellow = "#e5c07b",
            Blue = "#61afef", Magenta = "#c678dd", Cyan = "#56b6c2", White = "#abb2bf",
            BrightBlack = "#767676", BrightRed = "#e06c75", BrightGreen = "#98c379", BrightYellow = "#e5c07b",
            BrightBlue = "#61afef", BrightMagenta = "#c678dd", BrightCyan = "#56b6c2", BrightWhite = "#abb2bf"
        },
        new()
        {
            Name = "Rosé Pine",
            Foreground = "#e0def4", Background = "#191724", Cursor = "#555169", Selection = "#2a2837",
            Black = "#26233a", Red = "#eb6f92", Green = "#9ccfd8", Yellow = "#f6c177",
            Blue = "#31748f", Magenta = "#c4a7e7", Cyan = "#ebbcba", White = "#e0def4",
            BrightBlack = "#6e6a86", BrightRed = "#eb6f92", BrightGreen = "#9ccfd8", BrightYellow = "#f6c177",
            BrightBlue = "#31748f", BrightMagenta = "#c4a7e7", BrightCyan = "#ebbcba", BrightWhite = "#e0def4"
        },

        // Light themes
        new()
        {
            Name = "Solarized Light", IsDark = false,
            Foreground = "#657b83", Background = "#fdf6e3", Cursor = "#657b83", Selection = "#eee8d5",
            Black = "#002b36", Red = "#dc322f", Green = "#859900", Yellow = "#b58900",
            Blue = "#268bd2", Magenta = "#d33682", Cyan = "#2aa198", White = "#eee8d5",
            BrightBlack = "#073642", BrightRed = "#cb4b16", BrightGreen = "#586e75", BrightYellow = "#657b83",
            BrightBlue = "#839496", BrightMagenta = "#6c71c4", BrightCyan = "#93a1a1", BrightWhite = "#fdf6e3"
        },
        new()
        {
            Name = "Catppuccin Latte", IsDark = false,
            Foreground = "#4c4f69", Background = "#eff1f5", Cursor = "#dc8a78", Selection = "#acb0be",
            Black = "#bcc0cc", Red = "#d20f39", Green = "#40a02b", Yellow = "#df8e1d",
            Blue = "#1e66f5", Magenta = "#ea76cb", Cyan = "#179299", White = "#5c5f77",
            BrightBlack = "#acb0be", BrightRed = "#d20f39", BrightGreen = "#40a02b", BrightYellow = "#df8e1d",
            BrightBlue = "#1e66f5", BrightMagenta = "#ea76cb", BrightCyan = "#179299", BrightWhite = "#6c6f85"
        },
        new()
        {
            Name = "One Light", IsDark = false,
            Foreground = "#383a42", Background = "#f9f9f9", Cursor = "#383a42", Selection = "#e5e5e6",
            Black = "#000000", Red = "#e45649", Green = "#50a14f", Yellow = "#986801",
            Blue = "#4078f2", Magenta = "#a626a4", Cyan = "#0184bc", White = "#a0a1a7",
            BrightBlack = "#5c6370", BrightRed = "#e45649", BrightGreen = "#50a14f", BrightYellow = "#986801",
            BrightBlue = "#4078f2", BrightMagenta = "#a626a4", BrightCyan = "#0184bc", BrightWhite = "#ffffff"
        },
        new()
        {
            Name = "Gruvbox Light", IsDark = false,
            Foreground = "#3c3836", Background = "#fbf1c7", Cursor = "#3c3836", Selection = "#ebdbb2",
            Black = "#fbf1c7", Red = "#cc241d", Green = "#98971a", Yellow = "#d79921",
            Blue = "#458588", Magenta = "#b16286", Cyan = "#689d6a", White = "#7c6f64",
            BrightBlack = "#928374", BrightRed = "#9d0006", BrightGreen = "#79740e", BrightYellow = "#b57614",
            BrightBlue = "#076678", BrightMagenta = "#8f3f71", BrightCyan = "#427b58", BrightWhite = "#3c3836"
        },
        new()
        {
            Name = "Rosé Pine Dawn", IsDark = false,
            Foreground = "#575279", Background = "#faf4ed", Cursor = "#cecacd", Selection = "#dfdad9",
            Black = "#f2e9e1", Red = "#b4637a", Green = "#286983", Yellow = "#ea9d34",
            Blue = "#56949f", Magenta = "#907aa9", Cyan = "#d7827e", White = "#575279",
            BrightBlack = "#9893a5", BrightRed = "#b4637a", BrightGreen = "#286983", BrightYellow = "#ea9d34",
            BrightBlue = "#56949f", BrightMagenta = "#907aa9", BrightCyan = "#d7827e", BrightWhite = "#575279"
        },
        new()
        {
            Name = "Tokyo Night Day", IsDark = false,
            Foreground = "#3760bf", Background = "#e1e2e7", Cursor = "#3760bf", Selection = "#b7c1e3",
            Black = "#b4b5b9", Red = "#f52a65", Green = "#587539", Yellow = "#8c6c3e",
            Blue = "#2e7de9", Magenta = "#9854f1", Cyan = "#007197", White = "#6172b0",
            BrightBlack = "#a1a6c5", BrightRed = "#ff4774", BrightGreen = "#5c8524", BrightYellow = "#a27629",
            BrightBlue = "#358aff", BrightMagenta = "#a463ff", BrightCyan = "#007ea8", BrightWhite = "#3760bf"
        }
    ];

    public static TerminalTheme GetByName(string? name) =>
        BuiltIn.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? BuiltIn[0];
}
