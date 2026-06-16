using Avalonia;
using Avalonia.Controls;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.Views;

public partial class MainWindow : Window
{
    private SettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void BindWindowState(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = settingsService.Settings;

        if (s.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            if (!double.IsNaN(s.WindowWidth) && !double.IsNaN(s.WindowHeight))
            {
                Width = s.WindowWidth;
                Height = s.WindowHeight;
            }

            if (!double.IsNaN(s.WindowX) && !double.IsNaN(s.WindowY))
            {
                Position = new PixelPoint((int)s.WindowX, (int)s.WindowY);
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        SaveWindowState();
    }

    private void SaveWindowState()
    {
        if (_settingsService == null) return;
        var s = _settingsService.Settings;

        s.WindowMaximized = WindowState == WindowState.Maximized;

        if (WindowState == WindowState.Normal)
        {
            s.WindowX = Position.X;
            s.WindowY = Position.Y;
            s.WindowWidth = Width;
            s.WindowHeight = Height;
        }

        _settingsService.Save();
    }
}
