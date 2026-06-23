namespace mTerminal.Services;

public static class CrashHandler
{
    private static FileLogWriter? _writer;

    public static void Initialize(FileLogWriter writer)
    {
        _writer = writer;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public static void AttachAvaloniaExceptionHandler()
    {
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnAvaloniaUnhandledException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        _writer?.Write("FATAL", ex?.Message ?? "Unknown unhandled exception", ex?.ToString());
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _writer?.Write("ERROR", e.Exception.GetBaseException().Message, e.Exception.ToString());
        e.SetObserved();
    }

    private static void OnAvaloniaUnhandledException(object sender, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _writer?.Write("ERROR", e.Exception.Message, e.Exception.ToString());
        e.Handled = true;
    }
}
