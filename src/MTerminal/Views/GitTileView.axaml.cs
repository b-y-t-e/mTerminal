using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using MTerminal.Models;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class GitTileView : UserControl
{
    private bool _isVerticalLayout;
    private bool _layoutApplied;
    private GitTileViewModel? _subscribedVm;

    public GitTileView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        DataContextChanged += OnDataContextChanged;
        AddHandler(KeyDownEvent, OnFilesListKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnFilesListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space) return;
        if (DataContext is not GitTileViewModel vm) return;

        var selected = FilesListBox.SelectedItems;
        if (selected is not { Count: > 0 }) return;

        var files = selected.OfType<GitFileChange>().ToList();
        var newState = !files.All(f => f.IsChecked);
        foreach (var file in files)
            file.IsChecked = newState;

        vm.CommitCommand.NotifyCanExecuteChanged();
        e.Handled = true;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged -= OnVmPropertyChanged;

        if (DataContext is not GitTileViewModel vm) return;

        _subscribedVm = vm;
        vm.PropertyChanged += OnVmPropertyChanged;

        FontFamily = new FontFamily(vm.FontFamily);
        FontSize = vm.FontSize;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not GitTileViewModel vm) return;

        Dispatcher.UIThread.Post(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(GitTileViewModel.FontFamily):
                    FontFamily = new FontFamily(vm.FontFamily);
                    break;
                case nameof(GitTileViewModel.FontSize):
                    FontSize = vm.FontSize;
                    break;
                case nameof(GitTileViewModel.ShowDiffPanel):
                    RefreshLayout();
                    break;
            }
        });
    }

    private const double MinDiffSize = 250;
    private const double SidebarSize = 260;
    private const double MinCommitAreaHeight = 200;

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _lastSize = e.NewSize;
        RefreshLayout();
    }

    private Size _lastSize;

    private void RefreshLayout()
    {
        var w = _lastSize.Width;
        var h = _lastSize.Height;
        if (w == 0 && h == 0) return;

        var wantVertical = w < 480;
        var layoutChanged = !_layoutApplied || wantVertical != _isVerticalLayout;
        _isVerticalLayout = wantVertical;
        _layoutApplied = true;

        var userWantsDiff = (DataContext as GitTileViewModel)?.ShowDiffPanel ?? true;

        bool showDiff;
        if (!userWantsDiff)
            showDiff = false;
        else if (_isVerticalLayout)
            showDiff = h > MinDiffSize + MinDiffSize;
        else
            showDiff = w > SidebarSize + MinDiffSize;

        if (RightPanel.IsVisible != showDiff || MainSplitter.IsVisible != showDiff)
        {
            RightPanel.IsVisible = showDiff;
            MainSplitter.IsVisible = showDiff;
            layoutChanged = true;
        }

        var sidebarHeight = showDiff ? h / 2 : h;
        var showCommit = sidebarHeight > MinCommitAreaHeight;
        if (CommitArea.IsVisible != showCommit)
            CommitArea.IsVisible = showCommit;

        if (layoutChanged)
            ApplyLayout(showDiff);
    }

    private void ApplyLayout(bool showDiff)
    {
        MainGrid.ColumnDefinitions.Clear();
        MainGrid.RowDefinitions.Clear();

        if (_isVerticalLayout)
        {
            MainGrid.RowDefinitions.Add(new RowDefinition(showDiff ? new GridLength(1, GridUnitType.Star) : GridLength.Star) { MinHeight = 180 });
            MainGrid.RowDefinitions.Add(new RowDefinition(showDiff ? GridLength.Auto : new GridLength(0)));
            MainGrid.RowDefinitions.Add(new RowDefinition(showDiff ? new GridLength(1, GridUnitType.Star) : new GridLength(0)));

            Grid.SetColumn(Sidebar, 0);       Grid.SetRow(Sidebar, 0);
            Grid.SetColumn(MainSplitter, 0);  Grid.SetRow(MainSplitter, 1);
            Grid.SetColumn(RightPanel, 0);    Grid.SetRow(RightPanel, 2);

            MainSplitter.Width = double.NaN;
            MainSplitter.Height = 3;
            MainSplitter.ResizeDirection = GridResizeDirection.Rows;
        }
        else
        {
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition(showDiff ? new GridLength(SidebarSize, GridUnitType.Pixel) : GridLength.Star));
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition(showDiff ? GridLength.Auto : new GridLength(0)));
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition(showDiff ? GridLength.Star : new GridLength(0)));

            Grid.SetRow(Sidebar, 0);       Grid.SetColumn(Sidebar, 0);
            Grid.SetRow(MainSplitter, 0);  Grid.SetColumn(MainSplitter, 1);
            Grid.SetRow(RightPanel, 0);    Grid.SetColumn(RightPanel, 2);

            MainSplitter.Height = double.NaN;
            MainSplitter.Width = 3;
            MainSplitter.ResizeDirection = GridResizeDirection.Columns;
        }
    }
}
