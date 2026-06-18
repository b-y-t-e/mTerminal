using System.Text.RegularExpressions;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace MTerminal.Services;

public static class ImagePasteService
{
    public static readonly Regex ImageLineRegex = new(@"^!\[([^\]]*)\]\(([^)]+)\)$", RegexOptions.Compiled);

    public static async Task<Bitmap?> TryGetClipboardBitmapAsync(IClipboard clipboard)
    {
        try
        {
            return await clipboard.TryGetBitmapAsync();
        }
        catch
        {
            return null;
        }
    }

    public static string SaveBitmapToDirectory(Bitmap bitmap, string directory)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"img_{DateTime.Now:yyyyMMdd_HHmmss}_{Random.Shared.Next(1000):D3}.png";
        var fullPath = Path.Combine(directory, fileName);

        FileHelper.WriteWithRetry(fullPath, p => bitmap.Save(p));
        return fileName;
    }

    public static string GetMarkdownImageLink(string filename) => $"![]({filename})";

    private const int MaxCacheSize = 100;
    private static readonly Dictionary<string, Bitmap?> BitmapCache = new();
    private static readonly LinkedList<string> LruOrder = new();

    public static Bitmap? LoadBitmap(string path)
    {
        if (BitmapCache.TryGetValue(path, out var cached))
        {
            LruOrder.Remove(path);
            LruOrder.AddFirst(path);
            return cached;
        }

        Bitmap? bitmap = null;
        try
        {
            if (File.Exists(path))
                bitmap = new Bitmap(path);
        }
        catch { }

        while (BitmapCache.Count >= MaxCacheSize && LruOrder.Last != null)
        {
            var evict = LruOrder.Last.Value;
            LruOrder.RemoveLast();
            if (BitmapCache.Remove(evict, out var old))
                old?.Dispose();
        }

        BitmapCache[path] = bitmap;
        LruOrder.AddFirst(path);
        return bitmap;
    }

    public static void ClearBitmapCache()
    {
        foreach (var b in BitmapCache.Values)
            b?.Dispose();
        BitmapCache.Clear();
        LruOrder.Clear();
    }

    public static bool ContainsImageLink(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (ImageLineRegex.IsMatch(line.TrimEnd('\r')))
                return true;
        }
        return false;
    }

    public static string? ResolveAndCopyImage(string markdownLine, string todoDir)
    {
        var match = ImageLineRegex.Match(markdownLine);
        if (!match.Success) return null;

        var filePath = match.Groups[2].Value;

        if (Path.IsPathRooted(filePath))
        {
            if (File.Exists(filePath)) return filePath;
            return null;
        }

        var inTodo = Path.Combine(todoDir, filePath);
        if (File.Exists(inTodo))
            return Path.GetFullPath(inTodo);

        var notesDir = Path.Combine(Path.GetDirectoryName(todoDir) ?? todoDir, "notes");
        var inNotes = Path.Combine(notesDir, filePath);
        if (File.Exists(inNotes))
        {
            try
            {
                Directory.CreateDirectory(todoDir);
                var destPath = Path.Combine(todoDir, Path.GetFileName(filePath));
                if (!File.Exists(destPath))
                    File.Copy(inNotes, destPath);
                return Path.GetFullPath(destPath);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.TraceWarning("ResolveAndCopyImage failed: {0}", ex.Message);
                return Path.GetFullPath(inNotes);
            }
        }

        return null;
    }
}
