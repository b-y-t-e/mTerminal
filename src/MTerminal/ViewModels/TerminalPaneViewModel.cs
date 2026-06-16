using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using MTerminal.Models;

namespace MTerminal.ViewModels;

public partial class TerminalPaneViewModel : ObservableObject, IDisposable
{
    public string WorkingDirectory { get; }
    public ShellProfile Shell { get; }
    public TerminalTheme Theme { get; }

    internal Control? CachedControl { get; set; }
    internal bool IsLaunched { get; set; }

    public TerminalPaneViewModel(string workingDirectory, ShellProfile? shell = null, AppSettings? settings = null)
    {
        var s = settings ?? new AppSettings();
        WorkingDirectory = workingDirectory;
        Shell = shell ?? ShellProfile.ResolveDefault(s);
        Theme = TerminalTheme.GetByName(s.TerminalThemeName);
    }

    public void Dispose()
    {
        if (CachedControl is Iciclecreek.Terminal.TerminalControl tc)
        {
            try { tc.Kill(); } catch { }
        }
    }
}
