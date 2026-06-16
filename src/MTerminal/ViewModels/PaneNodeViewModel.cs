using CommunityToolkit.Mvvm.ComponentModel;

namespace MTerminal.ViewModels;

public abstract partial class PaneNodeViewModel : ObservableObject
{
    public PaneNodeViewModel? Parent { get; set; }
    public bool IsFirstChild => Parent is SplitPaneNodeViewModel split && split.First == this;

    public Action? LayoutChanged { get; set; }

    protected void NotifyLayoutChanged() => LayoutChanged?.Invoke();
}
