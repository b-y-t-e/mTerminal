using Avalonia.Controls;

namespace MTerminal.Views;

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
}
