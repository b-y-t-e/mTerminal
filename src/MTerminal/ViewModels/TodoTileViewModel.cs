using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class TodoTileViewModel : ObservableObject, IDisposable
{
    private static readonly Regex MdLineRegex = new(@"^(?:- )?\[([ xX])\] (.*)$", RegexOptions.Compiled);

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    [ObservableProperty]
    private double _checkSize = 20.0;

    [ObservableProperty]
    private Thickness _itemPadding = new(2, 1);

    public ObservableCollection<TodoItem> Items { get; } = [];

    private readonly string _filePath;
    private readonly SettingsService? _settingsService;
    private Timer? _saveTimer;
    private Timer? _reloadTimer;
    private bool _isLoading;
    private FileSystemWatcher? _watcher;
    private bool _hasPendingChanges;

    public string FilePath => _filePath;

    public TodoTileViewModel(string filePath, SettingsService? settingsService = null)
    {
        _filePath = filePath;
        _settingsService = settingsService;
        var s = settingsService?.Settings;
        _fontFamily = s?.NoteFontFamily ?? "Cascadia Mono, Consolas, monospace";
        _fontSize = s?.NoteFontSize ?? 14;
        UpdateSizeMetrics();
        _isLoading = true;
        LoadFromFile();
        _isLoading = false;

        if (Items.Count == 0)
            Items.Add(CreateItem());

        if (_settingsService != null)
            _settingsService.SettingsChanged += OnSettingsChanged;

        StartWatching();
    }

    private void UpdateSizeMetrics()
    {
        var scale = FontSize / 14.0;
        CheckSize = FontSize * 1.4;
        ItemPadding = new Thickness(3 * scale, 2 * scale);
    }

    private void OnSettingsChanged()
    {
        var s = _settingsService!.Settings;
        if (s.NoteFontFamily != FontFamily)
            FontFamily = s.NoteFontFamily;
        if (Math.Abs(s.NoteFontSize - FontSize) > 0.01)
        {
            FontSize = s.NoteFontSize;
            UpdateSizeMetrics();
        }
    }

    public string InsertItemAfter(int index)
    {
        var item = CreateItem();
        var insertAt = index + 1;

        var firstDoneIdx = -1;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].IsDone) { firstDoneIdx = i; break; }
        }

        if (firstDoneIdx >= 0 && insertAt > firstDoneIdx)
            insertAt = firstDoneIdx;

        Items.Insert(insertAt, item);
        ScheduleSave();
        return item.Id;
    }

    [RelayCommand]
    private void ToggleItem(string? id)
    {
        if (id == null) return;
        var idx = -1;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == id) { idx = i; break; }
        }
        if (idx < 0) return;

        var item = Items[idx];
        item.IsDone = !item.IsDone;
        Items.RemoveAt(idx);

        if (item.IsDone)
        {
            Items.Add(item);
        }
        else
        {
            var insertIdx = Items.Count;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].IsDone) { insertIdx = i; break; }
            }
            Items.Insert(insertIdx, item);
        }

        ScheduleSave();
    }

    [RelayCommand]
    private void RemoveItem(string? id)
    {
        if (id == null) return;
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == id)
            {
                Items.RemoveAt(i);
                if (Items.Count == 0)
                    Items.Add(CreateItem());
                ScheduleSave();
                return;
            }
        }
    }

    public void OnItemTextChanged()
    {
        ScheduleSave();
    }

    private static TodoItem CreateItem(string text = "")
    {
        return new TodoItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = text,
            IsDone = false
        };
    }

    private void ScheduleSave()
    {
        if (_isLoading) return;
        _hasPendingChanges = true;
        _saveTimer?.Dispose();
        _saveTimer = new Timer(_ =>
            Dispatcher.UIThread.Post(() =>
            {
                var snapshot = Items.ToList();
                Task.Run(() =>
                {
                    SaveToFile(snapshot);
                    _hasPendingChanges = false;
                });
            }), null, 1000, Timeout.Infinite);
    }

    private static List<TodoItem> ParseMarkdown(string[] lines)
    {
        var items = new List<TodoItem>();
        foreach (var line in lines)
        {
            var match = MdLineRegex.Match(line);
            if (match.Success)
            {
                items.Add(new TodoItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Text = match.Groups[2].Value,
                    IsDone = match.Groups[1].Value != " "
                });
            }
        }
        return items;
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            foreach (var item in ParseMarkdown(File.ReadAllLines(_filePath)))
                Items.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("TodoTile load failed: {0}", ex.Message);
        }
    }

    private void SaveToFile(List<TodoItem> snapshot)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var lines = snapshot.Select(item =>
                $"[{(item.IsDone ? "x" : " ")}] {item.Text}");
            if (_watcher != null) _watcher.EnableRaisingEvents = false;
            File.WriteAllLines(_filePath, lines);
            if (_watcher != null) _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("TodoTile save failed: {0}", ex.Message);
        }
    }

    private void StartWatching()
    {
        var dir = Path.GetDirectoryName(_filePath);
        var name = Path.GetFileName(_filePath);
        if (dir == null || name == null) return;

        try
        {
            Directory.CreateDirectory(dir);
            _watcher = new FileSystemWatcher(dir, name)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("TodoTile watcher failed: {0}", ex.Message);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_hasPendingChanges) return;

        _reloadTimer?.Dispose();
        _reloadTimer = new Timer(_ =>
            Dispatcher.UIThread.Post(ReloadFromFile), null, 500, Timeout.Infinite);
    }

    private void ReloadFromFile()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var newItems = ParseMarkdown(File.ReadAllLines(_filePath));

            _isLoading = true;
            Items.Clear();
            foreach (var item in newItems)
                Items.Add(item);
            if (Items.Count == 0)
                Items.Add(CreateItem());
            _isLoading = false;
        }
        catch (Exception ex)
        {
            _isLoading = false;
            System.Diagnostics.Trace.TraceWarning("TodoTile reload failed: {0}", ex.Message);
        }
    }

    public void Dispose()
    {
        if (_settingsService != null)
            _settingsService.SettingsChanged -= OnSettingsChanged;
        _watcher?.Dispose();
        _saveTimer?.Dispose();
        _reloadTimer?.Dispose();
        SaveToFile([.. Items]);
    }
}
