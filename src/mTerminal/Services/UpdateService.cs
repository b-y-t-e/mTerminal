using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Velopack;
using Velopack.Sources;

namespace mTerminal.Services;

public sealed class UpdateService
{
    private const string GithubRepo = "https://github.com/b-y-t-e/mTerminal";

    public async Task CheckAndPromptAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(GithubRepo, null, false));
            var newVersion = mgr.CheckForUpdates();
            if (newVersion == null)
                return;

            mgr.DownloadUpdates(newVersion);

            var shouldUpdate = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = desktop.MainWindow;
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
