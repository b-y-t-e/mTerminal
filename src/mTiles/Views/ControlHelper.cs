using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Iciclecreek.Terminal;

namespace mTiles.Views;

internal static class ControlHelper
{
    public static void DetachFromParent(Control control)
    {
        if (control.Parent is Panel panel)
            panel.Children.Remove(control);
        else if (control.Parent is ContentControl cc)
            cc.Content = null;
        else if (control.Parent is Decorator dec)
            dec.Child = null;
    }

    public static IBrush FindBrush(this StyledElement element, string key) =>
        element.FindResource(key) as IBrush
        ?? Application.Current?.FindResource(key) as IBrush
        ?? Brushes.Magenta;

    public static List<TerminalControl> SuspendTerminals(Control root)
    {
        var terminals = new List<TerminalControl>();
        CollectTerminals(root, terminals);
        foreach (var tc in terminals)
            tc.BeginReparent();
        return terminals;
    }

    public static void ResumeTerminals(List<TerminalControl> terminals)
    {
        foreach (var tc in terminals)
            tc.EndReparent();
    }

    private static void CollectTerminals(Control control, List<TerminalControl> result)
    {
        if (control is TerminalControl tc)
        {
            result.Add(tc);
            return;
        }

        if (control is ContentControl cc && cc.Content is Control child)
            CollectTerminals(child, result);
        else if (control is Decorator dec && dec.Child is Control decChild)
            CollectTerminals(decChild, result);
        else if (control is Panel panel)
        {
            foreach (var c in panel.Children)
            {
                if (c is Control ctrl)
                    CollectTerminals(ctrl, result);
            }
        }
    }
}
