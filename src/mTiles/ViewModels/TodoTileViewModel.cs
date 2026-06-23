using mTiles.Services;

namespace mTiles.ViewModels;

public class TodoTileViewModel(string filePath, SettingsService? settingsService = null)
    : MarkdownTileViewModel(filePath, settingsService);
