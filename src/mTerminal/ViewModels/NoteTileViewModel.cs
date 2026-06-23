using mTerminal.Services;

namespace mTerminal.ViewModels;

public class NoteTileViewModel(string filePath, SettingsService? settingsService = null)
    : MarkdownTileViewModel(filePath, settingsService);
