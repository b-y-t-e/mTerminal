using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MTerminal.ViewModels;

public partial class SplitTileNodeViewModel : TileNodeViewModel
{
    [ObservableProperty]
    private Orientation _orientation;

    [ObservableProperty]
    private double _splitRatio = 0.5;

    [ObservableProperty]
    private TileNodeViewModel? _first;

    [ObservableProperty]
    private TileNodeViewModel? _second;

    public SplitTileNodeViewModel(Orientation orientation, TileNodeViewModel first, TileNodeViewModel second)
    {
        _orientation = orientation;
        _first = first;
        _second = second;
    }

    partial void OnSplitRatioChanged(double value) => NotifyLayoutChanged();
}
