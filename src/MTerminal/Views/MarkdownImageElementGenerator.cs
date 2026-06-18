using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Rendering;
using MTerminal.Services;

namespace MTerminal.Views;

public class MarkdownImageElementGenerator : VisualLineElementGenerator
{
    private readonly string _baseDirectory;

    public MarkdownImageElementGenerator(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var line = CurrentContext.VisualLine;
        var docLine = line.FirstDocumentLine;

        if (startOffset > docLine.Offset)
            return -1;

        var lineText = CurrentContext.Document.GetText(docLine.Offset, docLine.Length);
        if (ImagePasteService.ImageLineRegex.IsMatch(lineText))
            return docLine.Offset;

        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var line = CurrentContext.VisualLine;
        var docLine = line.FirstDocumentLine;

        if (offset != docLine.Offset)
            return null;

        var lineText = CurrentContext.Document.GetText(docLine.Offset, docLine.Length);
        var match = ImagePasteService.ImageLineRegex.Match(lineText);
        if (!match.Success)
            return null;

        var filePath = match.Groups[2].Value;
        var fullPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_baseDirectory, filePath);

        var bitmap = ImagePasteService.LoadBitmap(fullPath);
        if (bitmap == null)
        {
            var placeholder = new TextBlock
            {
                Text = $"[image: {filePath}]",
                Foreground = Brushes.Gray,
                FontStyle = FontStyle.Italic,
                Margin = new Thickness(4, 2),
                IsHitTestVisible = false
            };
            return new InlineObjectElement(docLine.Length, placeholder);
        }

        var image = new Image
        {
            Source = bitmap,
            MaxWidth = 500,
            MaxHeight = 200,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(4, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsHitTestVisible = false
        };

        return new InlineObjectElement(docLine.Length, image);
    }
}
