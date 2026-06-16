using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class SplitPaneView : UserControl
{
    private SplitPaneNodeViewModel? _currentVm;
    private Orientation? _builtOrientation;

    public SplitPaneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm != null)
            _currentVm.PropertyChanged -= OnVmPropertyChanged;

        _currentVm = DataContext as SplitPaneNodeViewModel;
        _builtOrientation = null;

        if (_currentVm != null)
            _currentVm.PropertyChanged += OnVmPropertyChanged;

        BuildLayout();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SplitPaneNodeViewModel.Orientation))
            BuildLayout();
    }

    private void BuildLayout()
    {
        if (_currentVm is not { } vm)
        {
            Content = null;
            return;
        }

        if (_builtOrientation == vm.Orientation)
            return;

        _builtOrientation = vm.Orientation;

        var grid = new Grid();

        var first = new ContentControl();
        first.Bind(ContentControl.ContentProperty, new Binding(nameof(vm.First)) { Source = vm });

        var splitter = new GridSplitter
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
        };

        var second = new ContentControl();
        second.Bind(ContentControl.ContentProperty, new Binding(nameof(vm.Second)) { Source = vm });

        if (vm.Orientation == Orientation.Vertical)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(vm.SplitRatio, GridUnitType.Star)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Pixel)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1 - vm.SplitRatio, GridUnitType.Star)));

            Grid.SetColumn(first, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(second, 2);
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(vm.SplitRatio, GridUnitType.Star)));
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(3, GridUnitType.Pixel)));
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(1 - vm.SplitRatio, GridUnitType.Star)));

            Grid.SetRow(first, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(second, 2);
        }

        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);

        Content = grid;
    }
}
