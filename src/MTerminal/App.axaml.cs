using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MTerminal.Services;
using MTerminal.ViewModels;
using MTerminal.Views;

namespace MTerminal;

public partial class App : Application
{
    private SettingsService _settingsService = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _settingsService = new SettingsService();
        var workspaceService = new WorkspaceService();
        var persistenceService = new PersistenceService();

        var mainVm = new MainWindowViewModel(workspaceService, persistenceService, _settingsService);

        _settingsService.SettingsChanged += () =>
        {
            var theme = _settingsService.Settings.Theme;
            RequestedThemeVariant = theme == "Light"
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        };

        RequestedThemeVariant = _settingsService.Settings.Theme == "Light"
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.BindWindowState(_settingsService);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
