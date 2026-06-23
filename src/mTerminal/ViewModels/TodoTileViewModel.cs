using mTerminal.Services;

namespace mTerminal.ViewModels;

public class TodoTileViewModel(string filePath, SettingsService? settingsService = null)
    : MarkdownTileViewModel(filePath, settingsService);
