using CommunityToolkit.Mvvm.ComponentModel;

namespace MTerminal.ViewModels;

public abstract partial class TileNodeViewModel : ObservableObject
{
    public TileNodeViewModel? Parent { get; set; }
    public bool IsFirstChild => Parent is SplitTileNodeViewModel split && split.First == this;

    public Action? LayoutChanged { get; set; }

    protected void NotifyLayoutChanged() => LayoutChanged?.Invoke();
}
