using System.Diagnostics;
using Avalonia;
using mTerminal.Services;
using Velopack;

namespace mTerminal;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var logWriter = new FileLogWriter();
        CrashHandler.Initialize(logWriter);
        Trace.Listeners.Add(new LogTraceListener(logWriter));

        try
        {
            VelopackApp.Build().Run();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            logWriter.Write("FATAL", ex.Message, ex.ToString());
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .AfterSetup(_ => CrashHandler.AttachAvaloniaExceptionHandler());
}
