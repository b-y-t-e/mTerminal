using System.Diagnostics;
using mTiles.Models;

namespace mTiles.Services;

public static class FileHelper
{
    public static void OpenFolderAndSelect(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo("open", $"-R \"{filePath}\"") { UseShellExecute = true });
        }
        else
        {
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null)
                Process.Start(new ProcessStartInfo("xdg-open", dir) { UseShellExecute = true });
        }
    }

    public static void WriteWithRetry(string path, Action<string> writeAction)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);

        try
        {
            writeAction(path);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("File write failed, retrying: {0}", ex.Message);
            Thread.Sleep(AppDefaults.FileRetryDelayMs);
            try
            {
                writeAction(path);
            }
            catch (Exception ex2)
            {
                Trace.TraceWarning("File write retry failed: {0}", ex2.Message);
            }
        }
    }
}
