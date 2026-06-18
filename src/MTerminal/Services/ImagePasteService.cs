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
}
