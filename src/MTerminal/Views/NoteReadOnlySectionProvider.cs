using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using MTerminal.Services;

namespace MTerminal.Views;

public class NoteReadOnlySectionProvider : IReadOnlySectionProvider
{
    private readonly TextDocument _document;

    public NoteReadOnlySectionProvider(TextDocument document)
    {
        _document = document;
    }

    public bool CanInsert(int offset)
    {
        if (offset < 0 || offset > _document.TextLength) return true;
        var line = _document.GetLineByOffset(offset);
        var lineText = _document.GetText(line.Offset, line.Length);
        return !ImagePasteService.ImageLineRegex.IsMatch(lineText);
    }

    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
        int start = segment.Offset;
        int end = segment.EndOffset;

        if (start >= end)
        {
            yield return segment;
            yield break;
        }

        int pos = start;
        int currentStart = -1;

        while (pos < end)
        {
            var line = _document.GetLineByOffset(pos);
            var lineText = _document.GetText(line.Offset, line.Length);
            bool isImage = ImagePasteService.ImageLineRegex.IsMatch(lineText);
            int lineEnd = Math.Min(line.Offset + line.TotalLength, end);

            if (isImage)
            {
                bool fullySelected = start <= line.Offset && end >= line.EndOffset;

                if (fullySelected)
                {
                    if (currentStart < 0) currentStart = pos;
                }
                else
                {
                    if (currentStart >= 0)
                    {
                        yield return new TextSegment { StartOffset = currentStart, EndOffset = pos };
                        currentStart = -1;
                    }
                }
            }
            else
            {
                if (currentStart < 0) currentStart = pos;
            }

            pos = line.Offset + line.TotalLength;
            if (pos <= line.Offset) break;
        }

        if (currentStart >= 0)
            yield return new TextSegment { StartOffset = currentStart, EndOffset = Math.Min(end, _document.TextLength) };
    }
}
