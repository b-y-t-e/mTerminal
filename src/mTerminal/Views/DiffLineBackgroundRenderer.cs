using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace mTerminal.Views;

public class DiffLineBackgroundRenderer : IBackgroundRenderer
{
    private static readonly ISolidColorBrush AddedBg = new SolidColorBrush(Color.FromArgb(28, 115, 201, 145));
    private static readonly ISolidColorBrush RemovedBg = new SolidColorBrush(Color.FromArgb(28, 192, 104, 120));
    private static readonly ISolidColorBrush HunkBg = new SolidColorBrush(Color.FromArgb(20, 80, 140, 220));

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document == null) return;

        foreach (var line in textView.VisualLines)
        {
            var docLine = line.FirstDocumentLine;
            if (docLine == null) continue;

            var text = textView.Document.GetText(docLine.Offset, docLine.Length);
            if (text.Length == 0) continue;

            ISolidColorBrush? brush = text[0] switch
            {
                '+' when !text.StartsWith("+++") => AddedBg,
                '-' when !text.StartsWith("---") => RemovedBg,
                '@' => HunkBg,
                _ => null
            };

            if (brush == null) continue;

            foreach (var r in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, docLine.Length))
                drawingContext.FillRectangle(brush, new Rect(0, r.Top, textView.Bounds.Width, r.Height));
        }
    }
}
