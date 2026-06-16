using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MTerminal.ViewModels;

public partial class EditorPaneViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _text = string.Empty;

    private readonly string _filePath;
    private Timer? _saveTimer;
    private bool _isLoading;

    public string FilePath => _filePath;
    public string FontFamily { get; }
    public double FontSize { get; }

    internal Control? CachedControl { get; set; }

    public EditorPaneViewModel(string filePath, string? fontFamily = null, double? fontSize = null)
    {
        _filePath = filePath;
        FontFamily = fontFamily ?? "Cascadia Mono, Consolas, monospace";
        FontSize = fontSize ?? 14;
        _isLoading = true;
        LoadFromFile();
        _isLoading = false;
    }

    partial void OnTextChanged(string value)
    {
        if (_isLoading) return;
        _saveTimer?.Dispose();
        _saveTimer = new Timer(_ => SaveToFile(), null, 2000, Timeout.Infinite);
    }

    private void LoadFromFile()
    {
        if (File.Exists(_filePath))
        {
            try { Text = File.ReadAllText(_filePath); }
            catch { }
        }
    }

    private void SaveToFile()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, Text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("EditorPane save failed: {0}", ex.Message);
        }
    }

    public void Dispose()
    {
        _saveTimer?.Dispose();
        SaveToFile();
    }
}
