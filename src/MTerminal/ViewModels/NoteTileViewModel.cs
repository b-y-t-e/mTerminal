using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class NoteTileViewModel : ObservableObject, IFileContent, IDisposable
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    private string _filePath;
    private readonly SettingsService? _settingsService;
    private Timer? _saveTimer;
    private bool _isLoading;

    public string FilePath => _filePath;

    internal Control? CachedControl { get; set; }

    public NoteTileViewModel(string filePath, SettingsService? settingsService = null)
    {
        _filePath = filePath;
        _settingsService = settingsService;
        var s = settingsService?.Settings;
        _fontFamily = s?.NoteFontFamily ?? "Cascadia Mono, Consolas, monospace";
        _fontSize = s?.NoteFontSize ?? 14;
        _isLoading = true;
        LoadFromFile();
        _isLoading = false;

        if (_settingsService != null)
            _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        var s = _settingsService!.Settings;
        if (s.NoteFontFamily != FontFamily)
            FontFamily = s.NoteFontFamily;
        if (Math.Abs(s.NoteFontSize - FontSize) > 0.01)
            FontSize = s.NoteFontSize;
    }

    public void RenameFile(string newName)
    {
        var sanitized = IFileContent.SanitizeFileName(newName);
        if (string.IsNullOrEmpty(sanitized)) return;

        var dir = Path.GetDirectoryName(_filePath);
        if (dir == null) return;

        var newPath = Path.Combine(dir, sanitized + ".md");
        if (string.Equals(newPath, _filePath, StringComparison.OrdinalIgnoreCase)) return;

        _saveTimer?.Dispose();
        _saveTimer = null;
        SaveToFile(Text, _filePath);

        try
        {
            if (File.Exists(_filePath))
                File.Move(_filePath, newPath, overwrite: false);
            _filePath = newPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("NoteTile rename failed: {0}", ex.Message);
        }
    }

    partial void OnTextChanged(string value)
    {
        if (_isLoading) return;
        _saveTimer?.Dispose();
        var path = _filePath;
        var text = value;
        _saveTimer = new Timer(_ => SaveToFile(text, path), null, 2000, Timeout.Infinite);
    }

    private void LoadFromFile()
    {
        if (File.Exists(_filePath))
        {
            try { Text = File.ReadAllText(_filePath); }
            catch { }
        }
    }

    private void SaveToFile(string text, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(path, text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("NoteTile save failed, retrying: {0}", ex.Message);
            try
            {
                Thread.Sleep(500);
                File.WriteAllText(path, text);
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Trace.TraceWarning("NoteTile save retry failed: {0}", ex2.Message);
            }
        }
    }

    public void Dispose()
    {
        if (_settingsService != null)
            _settingsService.SettingsChanged -= OnSettingsChanged;
        _saveTimer?.Dispose();
        SaveToFile(Text, _filePath);
    }
}
