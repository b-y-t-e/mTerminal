using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using DiffPlex.DiffBuilder.Model;

namespace mTerminal.Views;

public class SideDiffBackgroundRenderer : IBackgroundRenderer
{
    private readonly bool _isOld;
    private IList<DiffPiece>? _lines;

    private static readonly ISolidColorBrush RemovedBg = new SolidColorBrush(Color.FromArgb(28, 192, 104, 120));
    private static readonly ISolidColorBrush AddedBg = new SolidColorBrush(Color.FromArgb(28, 115, 201, 145));
    private static readonly ISolidColorBrush ImaginaryBg = new SolidColorBrush(Color.FromArgb(12, 128, 128, 128));

    public SideDiffBackgroundRenderer(bool isOld) => _isOld = isOld;

    public void SetLines(IList<DiffPiece> lines) => _lines = lines;

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document == null || _lines == null) return;

        foreach (var line in textView.VisualLines)
        {
            var docLine = line.FirstDocumentLine;
            if (docLine == null) continue;

            var lineIndex = docLine.LineNumber - 1;
            if (lineIndex < 0 || lineIndex >= _lines.Count) continue;

            var diffLine = _lines[lineIndex];

            ISolidColorBrush? brush = diffLine.Type switch
            {
                ChangeType.Deleted => _isOld ? RemovedBg : null,
                ChangeType.Inserted => !_isOld ? AddedBg : null,
                ChangeType.Imaginary => ImaginaryBg,
                ChangeType.Modified => _isOld ? RemovedBg : AddedBg,
                _ => null
            };

            if (brush == null) continue;

            foreach (var r in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, docLine.Length))
                drawingContext.FillRectangle(brush, new Rect(0, r.Top, textView.Bounds.Width, r.Height));
        }
    }
}
