using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class EditorPaneView : UserControl
{
    public EditorPaneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not EditorPaneViewModel vm) return;

        if (vm.CachedControl is TextEditor cached)
        {
            if (cached.Parent is Panel p) p.Children.Remove(cached);
            else if (cached.Parent is ContentControl cc) cc.Content = null;
            else if (cached.Parent is Decorator d) d.Child = null;
            Content = cached;
            return;
        }

        var editor = new TextEditor
        {
            FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 13,
            ShowLineNumbers = true,
            WordWrap = true,
            Background = new SolidColorBrush(Color.Parse("#1a1a2e")),
            Foreground = new SolidColorBrush(Color.Parse("#c0c8e0"))
        };

        editor.Text = vm.Text;

        editor.Document.Changed += (_, _) => vm.Text = editor.Text;

        vm.CachedControl = editor;
        Content = editor;

        AttachedToVisualTree += OnceAttached;

        void OnceAttached(object? s, VisualTreeAttachmentEventArgs args)
        {
            AttachedToVisualTree -= OnceAttached;
            Dispatcher.UIThread.Post(() => editor.TextArea?.Focus(), DispatcherPriority.Input);
        }
    }
}
