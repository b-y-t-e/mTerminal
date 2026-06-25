using System.Diagnostics;
using Avalonia.Threading;
using Velopack;
using Velopack.Sources;

namespace mTiles.Services;

public sealed class UpdateService : IDisposable
{
    private const string GithubRepo = "https://github.com/b-y-t-e/mTiles";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);

    private readonly DispatcherTimer _timer;
    private readonly UpdateManager _mgr = new(new GithubSource(GithubRepo, null, false));
    private volatile UpdateInfo? _pendingUpdate;
    private int _checking;

    public event Action? UpdateAvailable;
    public bool HasUpdate => _pendingUpdate != null;
    public string? NewVersion => _pendingUpdate?.TargetFullRelease.Version.ToString();

    public UpdateService()
    {
        _timer = new DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += (_, _) => _ = Task.Run(() => CheckSilently());
    }

    public void StartPeriodicCheck()
    {
        _timer.Start();
        _ = Task.Run(() => CheckSilently());
    }

    private void CheckSilently()
    {
        if (_pendingUpdate != null) return;
        if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0) return;
        try
        {
            var info = _mgr.CheckForUpdates();
            if (info != null)
            {
                _mgr.DownloadUpdates(info);
                _pendingUpdate = info;
                Dispatcher.UIThread.Post(() => UpdateAvailable?.Invoke());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _checking, 0);
        }
    }

    public void ApplyUpdate()
    {
        if (_pendingUpdate == null) return;
        _mgr.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
