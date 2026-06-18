using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MTerminal.Models;

public sealed class TodoItem : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _text = string.Empty;
    private bool _isDone;
    private string? _imagePath;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set => SetField(ref _isDone, value);
    }

    public string? ImagePath
    {
        get => _imagePath;
        set => SetField(ref _imagePath, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
