using Avalonia.Controls;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class LeafPaneView : UserControl
{
    private object? _currentContentVm;

    public LeafPaneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LeafPaneNodeViewModel leaf)
            SetContent(leaf.Content);
    }

    private void SetContent(object? contentVm)
    {
        if (contentVm == _currentContentVm && ContentHost.Children.Count > 0)
            return;

        _currentContentVm = contentVm;
        ContentHost.Children.Clear();

        if (contentVm == null) return;

        UserControl view = contentVm switch
        {
            TerminalPaneViewModel => new TerminalPaneView { DataContext = contentVm },
            EditorPaneViewModel => new EditorPaneView { DataContext = contentVm },
            _ => throw new InvalidOperationException($"Unknown content type: {contentVm.GetType()}")
        };

        ContentHost.Children.Add(view);
    }
}
