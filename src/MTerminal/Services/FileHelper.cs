using System.Diagnostics;
using MTerminal.Models;

namespace MTerminal.Services;

public static class FileHelper
{
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
