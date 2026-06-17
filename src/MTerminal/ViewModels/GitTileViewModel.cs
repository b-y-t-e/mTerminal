using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class GitTileViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _branchName = "";

    [ObservableProperty]
    private string _worktreePath;

    [ObservableProperty]
    private GitFileChange? _selectedChange;

    [ObservableProperty]
    private string _diffText = "";

    [ObservableProperty]
    private string _oldContent = "";

    [ObservableProperty]
    private string _newContent = "";

    [ObservableProperty]
    private string _commitMessage = "";

    [ObservableProperty]
    private string _commitDescription = "";

    [ObservableProperty]
    private bool _showHistory;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _stashCount;

    [ObservableProperty]
    private bool _isGitRepo = true;

    [ObservableProperty]
    private bool _allChecked = true;

    [ObservableProperty]
    private bool _showDiffPanel = true;

    [ObservableProperty]
    private bool _splitDiff;

    [ObservableProperty]
    private bool _diffTrimIndent = true;

    [ObservableProperty]
    private bool _diffSkipEmptyLines = true;

    [ObservableProperty]
    private CommitLogEntry? _selectedCommit;

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    [ObservableProperty]
    private double _checkSize = 20.0;

    [ObservableProperty]
    private Thickness _itemPadding = new(2, 1);

    public ObservableCollection<GitFileChange> Changes { get; } = [];
    public ObservableCollection<CommitLogEntry> CommitLog { get; } = [];

    public Action? TileSettingsChanged { get; set; }

    private readonly SettingsService? _settingsService;
    private FileSystemWatcher? _watcher;
    private Timer? _refreshDebounce;
    private CancellationTokenSource? _refreshCts;

    public GitTileViewModel(string workingDirectory, SettingsService? settingsService = null)
    {
        _worktreePath = workingDirectory;
        _settingsService = settingsService;
        var s = settingsService?.Settings;
        _fontFamily = s?.FontFamily ?? "Cascadia Mono, Consolas, monospace";
        _fontSize = s?.FontSize ?? 14;
        _diffTrimIndent = s?.DiffTrimIndent ?? true;
        UpdateSizeMetrics();

        if (_settingsService != null)
            _settingsService.SettingsChanged += OnSettingsChanged;

        Dispatcher.UIThread.Post(async () =>
        {
            try { await RefreshAsync(); }
            catch (Exception ex) { Trace.TraceWarning("GitTile init refresh failed: {0}", ex.Message); }
        });
    }

    private void OnSettingsChanged()
    {
        var s = _settingsService!.Settings;
        if (s.FontFamily != FontFamily)
            FontFamily = s.FontFamily;
        if (Math.Abs(s.FontSize - FontSize) > 0.01)
        {
            FontSize = s.FontSize;
            UpdateSizeMetrics();
        }
    }

    private void UpdateSizeMetrics()
    {
        var scale = FontSize / 14.0;
        CheckSize = FontSize * 1.4;
        ItemPadding = new Thickness(3 * scale, 2 * scale);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var oldCts = _refreshCts;
        oldCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        oldCts?.Dispose();
        var ct = _refreshCts.Token;

        IsLoading = true;
        try
        {
            var checkResult = await RunGitAsync("rev-parse --is-inside-work-tree", throwOnError: false, ct);
            if (checkResult.Trim() != "true")
            {
                IsGitRepo = false;
                return;
            }
            IsGitRepo = true;

            var branchTask = RunGitAsync("rev-parse --abbrev-ref HEAD", ct);
            var statusTask = RunGitAsync("status --porcelain", ct);
            var logTask = RunGitAsync("log --oneline -30", ct);
            var stashTask = RunGitAsync("stash list", ct);

            await Task.WhenAll(branchTask, statusTask, logTask, stashTask);

            ct.ThrowIfCancellationRequested();

            BranchName = (await branchTask).Trim();

            var statusLines = (await statusTask).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var oldSelected = SelectedChange?.FilePath;
            Changes.Clear();
            foreach (var line in statusLines)
            {
                if (line.Length < 3) continue;
                var change = ParseStatusLine(line);
                if (change != null)
                    Changes.Add(change);
            }

            if (oldSelected != null)
                SelectedChange = Changes.FirstOrDefault(c => c.FilePath == oldSelected);

            CommitLog.Clear();
            foreach (var logLine in (await logTask).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var spaceIdx = logLine.IndexOf(' ');
                if (spaceIdx > 0)
                    CommitLog.Add(new CommitLogEntry { Hash = logLine[..spaceIdx], Message = logLine[(spaceIdx + 1)..] });
                else
                    CommitLog.Add(new CommitLogEntry { Hash = logLine, Message = "" });
            }

            var stashLines = (await stashTask).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            StashCount = stashLines.Length;

            StartWatching();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Trace.TraceWarning("GitTile refresh failed: {0}", ex.Message);
            IsGitRepo = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static GitFileChange? ParseStatusLine(string line)
    {
        if (line.Length < 3) return null;

        var x = line[0];
        var y = line[1];
        var rawPath = line[3..].Trim();

        char statusChar;
        if (x == '?' && y == '?')
            statusChar = '?';
        else if (x != ' ' && x != '?')
            statusChar = x;
        else
            statusChar = y;

        string? oldPath = null;
        string path;

        if (statusChar is 'R' or 'C')
        {
            var arrowIdx = rawPath.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrowIdx > 0)
            {
                oldPath = rawPath[..arrowIdx].Trim('"');
                path = rawPath[(arrowIdx + 4)..].Trim('"');
            }
            else
            {
                path = rawPath.Trim('"');
            }
        }
        else
        {
            path = rawPath.Trim('"');
        }

        var display = statusChar switch
        {
            'M' => "Modified",
            'A' => "Added",
            'D' => "Deleted",
            'R' => "Renamed",
            'C' => "Copied",
            'U' => "Unmerged",
            '?' => "Untracked",
            _ => statusChar.ToString()
        };

        return new GitFileChange
        {
            FilePath = path,
            OldFilePath = oldPath,
            Status = statusChar.ToString(),
            StatusDisplay = display
        };
    }

    private async Task LoadDiffForSelectedAsync()
    {
        var change = SelectedChange;
        if (change == null)
        {
            DiffText = "";
            OldContent = "";
            NewContent = "";
            return;
        }

        try
        {
            string diffArgs;
            if (change.Status == "?")
                diffArgs = $"diff --no-index /dev/null -- \"{change.FilePath}\"";
            else if (change.Status == "R" && change.OldFilePath != null)
                diffArgs = $"diff -M -- \"{change.OldFilePath}\" \"{change.FilePath}\"";
            else
                diffArgs = $"diff -- \"{change.FilePath}\"";
            var diffTask = RunGitAsync(diffArgs, throwOnError: false);

            Task<string> oldTask;
            Task<string> newTask;

            if (change.Status == "?")
            {
                oldTask = Task.FromResult("");
                newTask = ReadWorkingFileAsync(change.FilePath);
            }
            else if (change.Status == "D")
            {
                oldTask = RunGitAsync($"show HEAD:\"{change.FilePath}\"", throwOnError: false);
                newTask = Task.FromResult("");
            }
            else
            {
                var oldPath = change.OldFilePath ?? change.FilePath;
                oldTask = RunGitAsync($"show HEAD:\"{oldPath}\"", throwOnError: false);
                newTask = ReadWorkingFileAsync(change.FilePath);
            }

            await Task.WhenAll(diffTask, oldTask, newTask);

            var rawDiff = await diffTask;
            DiffText = StripDiffHeader(DiffTrimIndent ? TrimCommonIndent(rawDiff) : rawDiff);
            OldContent = await oldTask;
            NewContent = await newTask;
        }
        catch (Exception ex)
        {
            DiffText = $"Error loading diff: {ex.Message}";
            OldContent = "";
            NewContent = "";
        }
    }

    private static string StripDiffHeader(string diff)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var line in diff.Split('\n'))
        {
            if (line.StartsWith("diff --git") || line.StartsWith("index ") ||
                line.StartsWith("--- ") || line.StartsWith("+++ ") ||
                line.StartsWith("old mode") || line.StartsWith("new mode") ||
                line.StartsWith("similarity index") || line.StartsWith("rename from") ||
                line.StartsWith("rename to") || line.StartsWith("new file mode") ||
                line.StartsWith("deleted file mode"))
                continue;
            sb.AppendLine(line);
        }
        return sb.ToString();
    }

    private async Task<string> ReadWorkingFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(WorktreePath, relativePath);
        if (!File.Exists(fullPath)) return "";
        try { return await File.ReadAllTextAsync(fullPath); }
        catch { return ""; }
    }

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private async Task CommitAsync()
    {
        var checkedFiles = Changes.Where(c => c.IsChecked).Select(c => c.FilePath).ToList();
        if (checkedFiles.Count == 0 || string.IsNullOrWhiteSpace(CommitMessage)) return;

        IsLoading = true;
        try
        {
            var addArgs = string.Join(" ", checkedFiles.Select(f => $"\"{f}\""));
            await RunGitAsync($"add -- {addArgs}");

            var tempFile = Path.GetTempFileName();
            try
            {
                var fullMsg = CommitMessage;
                if (!string.IsNullOrWhiteSpace(CommitDescription))
                    fullMsg += "\n\n" + CommitDescription;
                await File.WriteAllTextAsync(tempFile, fullMsg);
                await RunGitAsync($"commit -F \"{tempFile}\"");
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }

            CommitMessage = "";
            CommitDescription = "";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("GitTile commit failed: {0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCommit() =>
        !string.IsNullOrWhiteSpace(CommitMessage) && Changes.Any(c => c.IsChecked);

    [RelayCommand]
    private async Task StashAsync()
    {
        IsLoading = true;
        try
        {
            await RunGitAsync("stash");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("GitTile stash failed: {0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StashPopAsync()
    {
        IsLoading = true;
        try
        {
            await RunGitAsync("stash pop");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("GitTile stash pop failed: {0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleHistory()
    {
        ShowHistory = !ShowHistory;
    }

    [RelayCommand]
    private void ToggleDiffPanel()
    {
        ShowDiffPanel = !ShowDiffPanel;
    }

    partial void OnShowDiffPanelChanged(bool value)
    {
        TileSettingsChanged?.Invoke();
    }

    [RelayCommand]
    private void ToggleSplitDiff()
    {
        SplitDiff = !SplitDiff;
    }

    [RelayCommand]
    private void ToggleDiffSkipEmptyLines()
    {
        DiffSkipEmptyLines = !DiffSkipEmptyLines;
    }

    [RelayCommand]
    private void ToggleAllChecked()
    {
        AllChecked = !AllChecked;
        foreach (var change in Changes)
            change.IsChecked = AllChecked;
        CommitCommand.NotifyCanExecuteChanged();
    }

    partial void OnCommitMessageChanged(string value)
    {
        CommitCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedChangeChanged(GitFileChange? value)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try { await LoadDiffForSelectedAsync(); }
            catch (Exception ex) { Trace.TraceWarning("GitTile load diff failed: {0}", ex.Message); }
        });
    }

    partial void OnSelectedCommitChanged(CommitLogEntry? value)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try { await LoadCommitDiffAsync(); }
            catch (Exception ex) { Trace.TraceWarning("GitTile load commit diff failed: {0}", ex.Message); }
        });
    }

    private async Task LoadCommitDiffAsync()
    {
        var commit = SelectedCommit;
        if (commit == null)
        {
            DiffText = "";
            OldContent = "";
            NewContent = "";
            return;
        }

        try
        {
            var diff = await RunGitAsync($"show --format= \"{commit.Hash}\"");
            DiffText = StripDiffHeader(DiffTrimIndent ? TrimCommonIndent(diff) : diff);
            OldContent = "";
            NewContent = "";
        }
        catch (Exception ex)
        {
            DiffText = $"Error: {ex.Message}";
        }
    }

    private void StartWatching()
    {
        if (_watcher != null) return;

        var gitDir = Path.Combine(WorktreePath, ".git");
        if (!Directory.Exists(gitDir)) return;

        try
        {
            _watcher = new FileSystemWatcher(gitDir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnGitDirChanged;
            _watcher.Created += OnGitDirChanged;
            _watcher.Deleted += OnGitDirChanged;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("GitTile watcher failed: {0}", ex.Message);
        }
    }

    private void OnGitDirChanged(object sender, FileSystemEventArgs e)
    {
        if (e.Name?.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) == true)
            return;

        _refreshDebounce?.Dispose();
        _refreshDebounce = new Timer(timerState =>
            Dispatcher.UIThread.Post(async () =>
            {
                try { await RefreshAsync(); }
                catch { }
            }),
            null, 500, Timeout.Infinite);
    }

    internal static string TrimCommonIndent(string diff)
    {
        var lines = diff.Split('\n');
        var minIndent = int.MaxValue;

        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            var prefix = line[0];
            if (prefix != ' ' && prefix != '+' && prefix != '-') continue;

            var content = line.Length > 1 ? line[1..] : "";
            if (content.Length == 0 || content.Trim().Length == 0) continue;
            if (line.StartsWith("+++") || line.StartsWith("---")) continue;

            var indent = 0;
            foreach (var c in content)
            {
                if (c == ' ') indent++;
                else if (c == '\t') indent += 4;
                else break;
            }
            if (indent < minIndent)
                minIndent = indent;
        }

        if (minIndent <= 0 || minIndent == int.MaxValue)
            return diff;

        var sb = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            if (line.Length > 1 && (line[0] == ' ' || line[0] == '+' || line[0] == '-')
                && !line.StartsWith("+++") && !line.StartsWith("---"))
            {
                var prefix = line[0];
                var content = line[1..];
                var trimmed = TrimLeading(content, minIndent);
                sb.Append(prefix).AppendLine(trimmed);
            }
            else
            {
                sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }

    private static string TrimLeading(string s, int count)
    {
        var removed = 0;
        var i = 0;
        while (i < s.Length && removed < count)
        {
            if (s[i] == ' ') { removed++; i++; }
            else if (s[i] == '\t') { removed += 4; i++; }
            else break;
        }
        return s[i..];
    }

    private Task<string> RunGitAsync(string arguments, CancellationToken ct = default)
        => RunGitAsync(arguments, throwOnError: true, ct);

    private async Task<string> RunGitAsync(string arguments, bool throwOnError, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = WorktreePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Failed to start git process: git {arguments}");

        using (process)
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(ct);

            if (throwOnError && process.ExitCode != 0)
            {
                var stderr = (await stderrTask).Trim();
                throw new InvalidOperationException(
                    $"git {arguments} failed (exit {process.ExitCode}): {stderr}");
            }

            return await stdoutTask;
        }
    }

    public void Dispose()
    {
        if (_settingsService != null)
            _settingsService.SettingsChanged -= OnSettingsChanged;
        _watcher?.Dispose();
        _refreshDebounce?.Dispose();
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
    }
}
