using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using MTerminal.Models;
using MTerminal.Services;
using MTerminal.ViewModels;
using MTerminal.Views;
using Velopack;

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
            ThemeBridge.Apply(TerminalTheme.GetByName(_settingsService.Settings.ColorThemeName));
            ApplyFontResources();
        };

        RequestedThemeVariant = _settingsService.Settings.Theme == "Light"
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        ThemeBridge.Apply(TerminalTheme.GetByName(_settingsService.Settings.ColorThemeName));
        ApplyFontResources();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.BindWindowState(_settingsService);
            desktop.MainWindow = mainWindow;
        }

        Task.Run(CheckForUpdates);

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyFontResources()
    {
        var s = _settingsService.Settings;
        Resources["UiFontFamily"] = new FontFamily(s.FontFamily);
        Resources["UiFontSize"] = s.FontSize;
        Resources["LogoFontSize"] = s.FontSize * 1.2;
    }

    private async Task CheckForUpdates()
    {
        try
        {
            var updateUrl = Environment.GetEnvironmentVariable("MTERMINAL_UPDATE_URL")
                            ?? "https://else.net.pl/mterminal/";
            var mgr = new UpdateManager(updateUrl);
            var newVersion = mgr.CheckForUpdates();
            if (newVersion == null)
                return;

            mgr.DownloadUpdates(newVersion);

            var shouldUpdate = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (window == null) return false;

                var box = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    "Update Available",
                    $"A new version ({newVersion.TargetFullRelease.Version}) is ready to install. Restart now to update?",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                    MsBox.Avalonia.Enums.Icon.Info);
                var result = await box.ShowWindowDialogAsync(window);
                return result == MsBox.Avalonia.Enums.ButtonResult.Yes;
            });

            if (shouldUpdate)
                mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
