using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;

namespace MTerminal.ViewModels;

public partial class LeafTileNodeViewModel : TileNodeViewModel
{
    [ObservableProperty]
    private ObservableObject? _content;

    [ObservableProperty]
    private TileContentType _contentType;

    [ObservableProperty]
    private string _tileName = "";

    [ObservableProperty]
    private bool _isActive;

    partial void OnTileNameChanged(string value) => NotifyLayoutChanged();

    private static event Action<LeafTileNodeViewModel>? ActiveTileChanged;

    private readonly Func<TileContentType, string, ObservableObject>? _contentFactory;
    private readonly Func<TileContentType, string>? _nameFactory;
    private readonly string _workingDirectory;

    public Action<TileNodeViewModel>? RootReplaced { get; set; }
    public Action? RootCleared { get; set; }

    public LeafTileNodeViewModel(TileContentType contentType, ObservableObject? content, string workingDirectory,
        Func<TileContentType, string, ObservableObject>? contentFactory = null,
        Func<TileContentType, string>? nameFactory = null)
    {
        _contentType = contentType;
        _content = content;
        _workingDirectory = workingDirectory;
        _contentFactory = contentFactory;
        _nameFactory = nameFactory;
        ActiveTileChanged += OnActiveTileChanged;
    }

    public void Activate() => ActiveTileChanged?.Invoke(this);

    private void OnActiveTileChanged(LeafTileNodeViewModel active) => IsActive = active == this;

    [RelayCommand]
    private void RestartTerminal()
    {
        if (Content is not TerminalTileViewModel tvm) return;
        if (tvm.CachedControl is not Iciclecreek.Terminal.TerminalControl tc) return;

        tc.Kill();
        _ = tc.LaunchProcess(_workingDirectory, tvm.Shell.ExecutablePath, tvm.Shell.Args);
    }

    [RelayCommand]
    private void SplitHorizontal() => Split(Orientation.Horizontal);

    [RelayCommand]
    private void SplitVertical() => Split(Orientation.Vertical);

    [RelayCommand]
    private void SelectContentType(TileContentType type)
    {
        if (ContentType != TileContentType.Empty) return;

        var newContent = _contentFactory?.Invoke(type, _workingDirectory);
        if (newContent == null) return;

        Content = newContent;
        ContentType = type;
        TileName = _nameFactory?.Invoke(type) ?? type.ToString();
        (newContent as IFileContent)?.RenameFile(TileName);
        NotifyLayoutChanged();
    }

    private void Split(Orientation orientation)
    {
        var newLeaf = new LeafTileNodeViewModel(TileContentType.Empty, null, _workingDirectory, _contentFactory, _nameFactory)
        {
            TileName = "",
            LayoutChanged = LayoutChanged,
            RootReplaced = RootReplaced,
            RootCleared = RootCleared
        };

        var oldParent = Parent;

        var split = new SplitTileNodeViewModel(orientation, this, newLeaf)
        {
            Parent = oldParent,
            LayoutChanged = LayoutChanged
        };

        this.Parent = split;
        newLeaf.Parent = split;

        if (oldParent is SplitTileNodeViewModel parentSplit)
        {
            if (parentSplit.First == this)
                parentSplit.First = split;
            else
                parentSplit.Second = split;
        }
        else
        {
            RootReplaced?.Invoke(split);
        }

        NotifyLayoutChanged();
    }

    [RelayCommand]
    private void Close()
    {
        ActiveTileChanged -= OnActiveTileChanged;

        if (Content is IDisposable disposable)
            disposable.Dispose();

        if (Parent is not SplitTileNodeViewModel parentSplit)
        {
            RootCleared?.Invoke();
            return;
        }

        var sibling = (this == parentSplit.First) ? parentSplit.Second : parentSplit.First;
        if (sibling == null) { RootCleared?.Invoke(); return; }

        sibling.Parent = parentSplit.Parent;
        sibling.LayoutChanged = LayoutChanged;
        PropagateSiblingCallbacks(sibling);

        if (parentSplit.Parent is SplitTileNodeViewModel grandParent)
        {
            if (parentSplit == grandParent.First)
                grandParent.First = sibling;
            else
                grandParent.Second = sibling;
        }
        else
        {
            RootReplaced?.Invoke(sibling);
        }

        NotifyLayoutChanged();
    }

    private void PropagateSiblingCallbacks(TileNodeViewModel node)
    {
        if (node is LeafTileNodeViewModel leaf)
        {
            leaf.RootReplaced = RootReplaced;
            leaf.RootCleared = RootCleared;
            leaf.LayoutChanged = LayoutChanged;
        }
        else if (node is SplitTileNodeViewModel split)
        {
            split.LayoutChanged = LayoutChanged;
            if (split.First != null) PropagateSiblingCallbacks(split.First);
            if (split.Second != null) PropagateSiblingCallbacks(split.Second);
        }
    }
}
