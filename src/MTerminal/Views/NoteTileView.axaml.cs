using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class NoteTileView : UserControl
{
    public NoteTileView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not NoteTileViewModel vm) return;

        if (vm.CachedControl is TextEditor cached)
        {
            ControlHelper.DetachFromParent(cached);
            Content = cached;
            return;
        }

        var editor = new TextEditor
        {
            FontFamily = new FontFamily(vm.FontFamily),
            FontSize = vm.FontSize,
            ShowLineNumbers = false,
            WordWrap = true,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Background = this.FindBrush("BgBase"),
            Foreground = this.FindBrush("TextPrimary")
        };

        editor.Text = vm.Text;

        editor.Document.Changed += (_, _) => vm.Text = editor.Text;

        vm.CachedControl = editor;
        Content = editor;

        AttachedToVisualTree += OnceAttached;

        void OnceAttached(object? s, VisualTreeAttachmentEventArgs args)
        {
            AttachedToVisualTree -= OnceAttached;
            Dispatcher.UIThread.Post(() =>
            {
                editor.TextArea?.Focus();
                StyleScrollBars(editor);
            }, DispatcherPriority.Loaded);
        }
    }

    private static void StyleScrollBars(TextEditor editor)
    {
        foreach (var sb in FindAll<ScrollBar>(editor))
        {
            sb.Width = 8;
            sb.MinWidth = 8;
            sb.Background = Brushes.Transparent;
            sb.Transitions = null;

            // Fluent theme animates Grid columns inside the template —
            // find the Grid and lock all column widths so nothing collapses.
            foreach (var grid in FindAll<Grid>(sb))
            {
                grid.Transitions = null;
                foreach (var col in grid.ColumnDefinitions)
                    col.Width = new GridLength(col.Width.Value == 0 ? 0 : 8, GridUnitType.Pixel);
                foreach (var row in grid.RowDefinitions)
                    row.Height = new GridLength(row.Height.Value == 0 ? 0 : 8, GridUnitType.Pixel);
            }
        }
        foreach (var thumb in FindAll<Thumb>(editor))
        {
            thumb.Width = 8;
            thumb.MinWidth = 8;
            thumb.Transitions = null;
        }
    }

    private static List<T> FindAll<T>(Visual root) where T : Visual
    {
        var result = new List<T>();
        var stack = new Stack<Visual>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is T match)
                result.Add(match);
            foreach (var child in current.GetVisualChildren())
                if (child is Visual v) stack.Push(v);
        }
        return result;
    }
}
