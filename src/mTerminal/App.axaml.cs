using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using mTerminal.Models;
using mTerminal.Services;
using mTerminal.Services.Database;
using mTerminal.ViewModels;
using mTerminal.Views;

namespace mTerminal;

public partial class App : Application
{
    private SettingsService _settingsService = null!;
    private DatabaseServiceManager? _dbManager;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _settingsService = new SettingsService();
        var workspaceService = new WorkspaceService();
        var persistenceService = new PersistenceService();

        _dbManager = new DatabaseServiceManager(_settingsService);
        if (_settingsService.Settings.Database.Enabled)
            _dbManager.Start();

        var mainVm = new MainWindowViewModel(workspaceService, persistenceService, _settingsService, _dbManager);

        _settingsService.SettingsChanged += () =>
        {
            var colorTheme = TerminalTheme.GetByName(_settingsService.Settings.ColorThemeName);
            RequestedThemeVariant = colorTheme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
            ThemeBridge.Apply(colorTheme);
            ApplyFontResources();
        };

        var initialColorTheme = TerminalTheme.GetByName(_settingsService.Settings.ColorThemeName);
        RequestedThemeVariant = initialColorTheme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        ThemeBridge.Apply(initialColorTheme);
        ApplyFontResources();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.BindWindowState(_settingsService);
            desktop.MainWindow = mainWindow;

            desktop.ShutdownRequested += (_, _) => _dbManager?.Dispose();

            var updateService = new UpdateService();
            _ = Task.Run(() => updateService.CheckAndPromptAsync(desktop));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyFontResources()
    {
        var s = _settingsService.Settings;
        Resources["UiFontFamily"] = new FontFamily(s.FontFamily);
        Resources["UiFontSize"] = s.FontSize;
        Resources["LogoFontSize"] = s.FontSize * AppDefaults.LogoFontSizeRatio;
        Resources["UiFontSizeSm"] = s.FontSize * AppDefaults.SmallFontSizeRatio;
    }
}
