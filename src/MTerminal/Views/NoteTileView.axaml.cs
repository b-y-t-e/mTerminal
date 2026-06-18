using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using MTerminal.Services;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class NoteTileView : UserControl
{
    private NoteTileViewModel? _subscribedVm;

    public NoteTileView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged -= OnVmPropertyChanged;

        if (DataContext is not NoteTileViewModel vm) return;

        _subscribedVm = vm;
        vm.PropertyChanged += OnVmPropertyChanged;

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
            Padding = new Thickness(8, 8),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            Foreground = Brushes.White
        };

        editor.Bind(TextEditor.BackgroundProperty, editor.GetResourceObservable("BgBase"));
        editor.Bind(TextEditor.ForegroundProperty, editor.GetResourceObservable("TextPrimary"));

        editor.Text = vm.Text;
        editor.Document.Changed += (_, _) => vm.Text = editor.Text;

        var baseDir = Path.GetDirectoryName(vm.FilePath) ?? ".";
        editor.TextArea.TextView.ElementGenerators.Add(new MarkdownImageElementGenerator(baseDir));
        editor.TextArea.TextView.Options.AllowScrollBelowDocument = false;
        editor.TextArea.ReadOnlySectionProvider = new NoteReadOnlySectionProvider(editor.Document);

        editor.TextArea.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);

        vm.CachedControl = editor;
        Content = editor;

        AttachedToVisualTree += OnceAttached;

        void OnceAttached(object? s, VisualTreeAttachmentEventArgs args)
        {
            AttachedToVisualTree -= OnceAttached;
            Dispatcher.UIThread.Post(() => editor.TextArea?.Focus(), DispatcherPriority.Input);
        }
    }

    private async void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not NoteTileViewModel vm) return;
        if (vm.CachedControl is not TextEditor editor) return;
        var doc = editor.Document;

        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard == null) return;

                var bitmap = await ImagePasteService.TryGetClipboardBitmapAsync(clipboard);
                if (bitmap == null)
                {
                    var text = await clipboard.TryGetTextAsync();
                    if (text != null)
                    {
                        if (ContainsImageLink(text))
                            InsertTextWithImageLinks(editor, text);
                        else
                            editor.TextArea.Selection.ReplaceSelectionWithText(text);
                    }
                    return;
                }

                using (bitmap)
                {
                    var dir = Path.GetDirectoryName(vm.FilePath) ?? ".";
                    var fileName = ImagePasteService.SaveBitmapToDirectory(bitmap, dir);
                    var mdLink = ImagePasteService.GetMarkdownImageLink(fileName);
                    InsertImageLink(editor, mdLink);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("NoteTile paste failed: {0}", ex.Message);
            }
        }
        else if (e.Key == Key.Back)
        {
            if (TryDeleteImageLine(editor, before: true))
                e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            if (TryDeleteImageLine(editor, before: false))
                e.Handled = true;
        }
        else if (!e.Handled && e.KeyModifiers == KeyModifiers.None && IsCharacterKey(e))
        {
            var line = doc.GetLineByOffset(editor.CaretOffset);
            var lineText = doc.GetText(line.Offset, line.Length);

            if (ImagePasteService.ImageLineRegex.IsMatch(lineText))
            {
                e.Handled = true;
                var nextLine = line.NextLine;
                if (nextLine != null)
                    editor.CaretOffset = nextLine.Offset;
                else
                {
                    doc.Insert(doc.TextLength, "\n");
                    editor.CaretOffset = doc.TextLength;
                }
            }
        }
    }

    private static bool ContainsImageLink(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (ImagePasteService.ImageLineRegex.IsMatch(line.TrimEnd('\r')))
                return true;
        }
        return false;
    }

    private static void InsertTextWithImageLinks(TextEditor editor, string text)
    {
        var doc = editor.Document;
        var offset = editor.CaretOffset;
        var currentLine = doc.GetLineByOffset(offset);

        var prefix = offset > currentLine.Offset ? "\n" : "";
        var suffix = offset < currentLine.EndOffset ? "\n" : "";

        var insertText = prefix + text.TrimEnd('\r', '\n') + suffix;
        doc.Insert(offset, insertText);
        editor.CaretOffset = offset + insertText.Length;
    }

    private static void InsertImageLink(TextEditor editor, string mdLink)
    {
        var doc = editor.Document;
        var offset = editor.CaretOffset;
        var currentLine = doc.GetLineByOffset(offset);

        string insertText;
        if (currentLine.Length == 0)
            insertText = $"{mdLink}\n";
        else
            insertText = $"\n{mdLink}\n";

        doc.Insert(offset, insertText);
        editor.CaretOffset = offset + insertText.Length;
    }

    private bool TryDeleteImageLine(TextEditor editor, bool before)
    {
        var doc = editor.Document;
        var offset = editor.CaretOffset;
        var currentLine = doc.GetLineByOffset(offset);

        DocumentLine? targetLine;

        if (before)
        {
            if (offset != currentLine.Offset) return false;
            targetLine = currentLine.PreviousLine;
        }
        else
        {
            if (offset != currentLine.EndOffset) return false;
            targetLine = currentLine.NextLine;
        }

        if (targetLine == null) return false;

        var targetText = doc.GetText(targetLine.Offset, targetLine.Length);
        if (!ImagePasteService.ImageLineRegex.IsMatch(targetText)) return false;

        int removeStart, removeLen;

        if (before)
        {
            removeStart = targetLine.Offset;
            removeLen = targetLine.TotalLength;
        }
        else
        {
            removeStart = currentLine.EndOffset;
            removeLen = (targetLine.NextLine != null
                ? targetLine.Offset + targetLine.TotalLength
                : targetLine.EndOffset) - currentLine.EndOffset;
        }

        removeStart = Math.Max(0, removeStart);
        removeLen = Math.Min(removeLen, doc.TextLength - removeStart);

        if (removeLen > 0)
            doc.Remove(removeStart, removeLen);

        return true;
    }

    private static bool IsCharacterKey(KeyEventArgs e)
    {
        return e.Key is >= Key.A and <= Key.Z
            or >= Key.D0 and <= Key.D9
            or >= Key.NumPad0 and <= Key.NumPad9
            or Key.Space or Key.OemPeriod or Key.OemComma
            or Key.OemMinus or Key.OemPlus
            or Key.Oem1 or Key.Oem2 or Key.Oem3 or Key.Oem4
            or Key.Oem5 or Key.Oem6 or Key.Oem7 or Key.Oem8
            or Key.OemOpenBrackets or Key.OemCloseBrackets
            or Key.OemPipe or Key.OemQuotes or Key.OemBackslash
            or Key.OemTilde;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not NoteTileViewModel vm) return;
        if (vm.CachedControl is not TextEditor editor) return;

        Dispatcher.UIThread.Post(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(NoteTileViewModel.FontFamily):
                    editor.FontFamily = new FontFamily(vm.FontFamily);
                    break;
                case nameof(NoteTileViewModel.FontSize):
                    editor.FontSize = vm.FontSize;
                    break;
            }
        });
    }
}
