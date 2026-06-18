using MTerminal.Models;

namespace MTerminal.Services;

public static class ShellDetector
{
    private static Dictionary<string, ShellType>? _typeLookup;

    public static ShellType GetTypeByName(string shellName)
    {
        _typeLookup ??= Detect().ToDictionary(s => s.Name, s => s.Type, StringComparer.OrdinalIgnoreCase);
        return _typeLookup.TryGetValue(shellName, out var t) ? t : ShellType.Other;
    }

    public static List<ShellProfile> Detect()
    {
        var profiles = new List<ShellProfile>();

        if (OperatingSystem.IsWindows())
        {
            var gitBashPaths = new[]
            {
                @"C:\Program Files\Git\bin\bash.exe",
                @"C:\Program Files (x86)\Git\bin\bash.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "bin", "bash.exe")
            };
            foreach (var path in gitBashPaths)
            {
                if (File.Exists(path))
                {
                    profiles.Add(new ShellProfile { Name = "Git Bash", ExecutablePath = path, Args = ["--login", "-i"], Type = ShellType.Bash });
                    break;
                }
            }

            var pwsh = FindExecutable("pwsh.exe")
                       ?? FindExecutable("powershell.exe");
            if (pwsh != null)
                profiles.Add(new ShellProfile { Name = "PowerShell", ExecutablePath = pwsh, Type = ShellType.PowerShell });

            var cmd = FindExecutable("cmd.exe");
            if (cmd != null)
                profiles.Add(new ShellProfile { Name = "CMD", ExecutablePath = cmd, Type = ShellType.Cmd });
        }
        else
        {
            var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
            var shellName = Path.GetFileNameWithoutExtension(shell);
            profiles.Add(new ShellProfile { Name = Path.GetFileName(shell), ExecutablePath = shell, Args = ["-l"], Type = InferType(shell) });

            if (shellName != "bash" && File.Exists("/bin/bash"))
                profiles.Add(new ShellProfile { Name = "bash", ExecutablePath = "/bin/bash", Args = ["-l"], Type = ShellType.Bash });

            if (shellName != "zsh" && File.Exists("/bin/zsh"))
                profiles.Add(new ShellProfile { Name = "zsh", ExecutablePath = "/bin/zsh", Args = ["-l"], Type = ShellType.Zsh });

            var fishPath = File.Exists("/usr/bin/fish") ? "/usr/bin/fish" : File.Exists("/bin/fish") ? "/bin/fish" : null;
            if (fishPath != null && shellName != "fish")
                profiles.Add(new ShellProfile { Name = "fish", ExecutablePath = fishPath, Args = ["-l"], Type = ShellType.Fish });
        }

        return profiles;
    }

    public static ShellProfile ResolveDefault(AppSettings settings)
    {
        var detected = Detect();

        if (!string.IsNullOrEmpty(settings.CustomShellPath))
        {
            var args = string.IsNullOrWhiteSpace(settings.CustomShellArgs)
                ? []
                : settings.CustomShellArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new ShellProfile
            {
                Name = "Custom",
                ExecutablePath = settings.CustomShellPath,
                Args = args,
                Type = settings.CustomShellType
            };
        }

        if (!string.IsNullOrEmpty(settings.DefaultShellName))
        {
            var match = detected.FirstOrDefault(s =>
                s.Name.Equals(settings.DefaultShellName, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        var fallbackExe = OperatingSystem.IsWindows() ? "powershell.exe" : "bash";
        return detected.FirstOrDefault()
            ?? new ShellProfile { Name = "Default", ExecutablePath = fallbackExe, Type = InferType(fallbackExe) };
    }

    public static ShellProfile ResolveFromUserProfile(UserShellProfile userProfile, AppSettings settings)
    {
        var detected = Detect();
        var match = detected.FirstOrDefault(s =>
            s.Name.Equals(userProfile.ShellName, StringComparison.OrdinalIgnoreCase));
        return match ?? ResolveDefault(settings);
    }

    public static ShellType InferType(string executablePath)
    {
        var name = Path.GetFileNameWithoutExtension(executablePath).ToLowerInvariant();
        return name switch
        {
            "pwsh" or "powershell" => ShellType.PowerShell,
            "cmd" => ShellType.Cmd,
            "bash" or "sh" => ShellType.Bash,
            "zsh" => ShellType.Zsh,
            "fish" => ShellType.Fish,
            _ => ShellType.Other
        };
    }

    private static string? FindExecutable(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir, name);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}
