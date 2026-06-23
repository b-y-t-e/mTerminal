using mTiles.Services;

namespace mTiles.ViewModels;

public class NoteTileViewModel(string filePath, SettingsService? settingsService = null)
    : MarkdownTileViewModel(filePath, settingsService);
