using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MTerminal.ViewModels;

public partial class SplitPaneNodeViewModel : PaneNodeViewModel
{
    [ObservableProperty]
    private Orientation _orientation;

    [ObservableProperty]
    private double _splitRatio = 0.5;

    [ObservableProperty]
    private PaneNodeViewModel? _first;

    [ObservableProperty]
    private PaneNodeViewModel? _second;

    public SplitPaneNodeViewModel(Orientation orientation, PaneNodeViewModel first, PaneNodeViewModel second)
    {
        _orientation = orientation;
        _first = first;
        _second = second;
    }

    partial void OnSplitRatioChanged(double value) => NotifyLayoutChanged();
}
