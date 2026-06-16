using CommunityToolkit.Mvvm.ComponentModel;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public static string[] Themes { get; } = ["Dark", "Light"];

    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _terminalFontFamily;

    [ObservableProperty]
    private double _terminalFontSize;

    [ObservableProperty]
    private string _editorFontFamily;

    [ObservableProperty]
    private double _editorFontSize;

    [ObservableProperty]
    private string _theme;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = settingsService.Settings;
        _terminalFontFamily = s.TerminalFontFamily;
        _terminalFontSize = s.TerminalFontSize;
        _editorFontFamily = s.EditorFontFamily;
        _editorFontSize = s.EditorFontSize;
        _theme = s.Theme;
    }

    partial void OnTerminalFontFamilyChanged(string value) { _settingsService.Settings.TerminalFontFamily = value; _settingsService.NotifyChanged(); }
    partial void OnTerminalFontSizeChanged(double value) { _settingsService.Settings.TerminalFontSize = value; _settingsService.NotifyChanged(); }
    partial void OnEditorFontFamilyChanged(string value) { _settingsService.Settings.EditorFontFamily = value; _settingsService.NotifyChanged(); }
    partial void OnEditorFontSizeChanged(double value) { _settingsService.Settings.EditorFontSize = value; _settingsService.NotifyChanged(); }
    partial void OnThemeChanged(string value) { _settingsService.Settings.Theme = value; _settingsService.NotifyChanged(); }
}
