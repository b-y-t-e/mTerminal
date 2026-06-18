using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Iciclecreek.Terminal;

namespace MTerminal.Views;

// Workaround for Iciclecreek.Terminal keyboard limitations:
// - TerminalView.OnKeyDown is a class handler (always runs, ignores e.Handled)
// - It clears selection on ANY keypress (including modifier-only like Ctrl)
// - It doesn't send ESC prefix for Alt+key combinations
// - WriterStream is not exposed publicly (requires reflection)
public sealed class TerminalKeyHandler
{
    private TerminalControl? _terminalControl;
    private TerminalView? _terminalView;
    private string? _lastSelectedText;
    private bool _registered;

    public void Attach(Control parent, TerminalControl tc)
    {
        var tv = tc.GetVisualDescendants().OfType<TerminalView>().FirstOrDefault();
        if (tv == null || tv == _terminalView) return;
        _terminalControl = tc;
        _terminalView = tv;

        // Pre-capture selection text on mouse release — TerminalView clears it on
        // the next keydown (even Ctrl alone), so it's gone before Ctrl+C arrives.
        tv.AddHandler(
            InputElement.PointerReleasedEvent,
            (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _lastSelectedText = tv.Terminal?.Selection?.HasSelection == true
                        ? tv.Terminal.Selection.GetSelectionText()
                        : null;
                }, DispatcherPriority.Background);
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        if (!_registered)
        {
            parent.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            _registered = true;
        }
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (_terminalControl == null || _terminalView == null) return;

            if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
            {
                e.Handled = true;
                await _terminalView.PasteAsync();
            }
            // TerminalView doesn't generate ESC prefix for Alt — sends raw char or nothing.
            // We write ESC+char directly; class handler won't duplicate (TryGetPrintableChar
            // fails with Alt modifier, so it sends nothing).
            else if (e.KeyModifiers == KeyModifiers.Alt && TryGetChar(e, out var ch))
            {
                e.Handled = true;
                PtyWriter.Write(_terminalControl, $"\x1b{ch}");
            }
            else if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control && !string.IsNullOrEmpty(_lastSelectedText))
            {
                e.Handled = true;
                var topLevel = TopLevel.GetTopLevel(_terminalView);
                if (topLevel?.Clipboard != null)
                    await topLevel.Clipboard.SetTextAsync(_lastSelectedText);
                _lastSelectedText = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Terminal key handler failed: {0}", ex.Message);
        }
    }

    private static bool TryGetChar(KeyEventArgs e, out char ch)
    {
        ch = '\0';
        var keyStr = e.Key.ToString();
        if (keyStr.Length == 1 && char.IsLetterOrDigit(keyStr[0]))
        {
            ch = char.ToLower(keyStr[0]);
            return true;
        }
        return false;
    }
}
