using System.Diagnostics;
using MTerminal.Models;

namespace MTerminal.Services;

public static class AiToolDetector
{
    private static readonly AiToolInfo[] KnownTools =
    [
        new() { Name = "Aider", BinaryName = "aider", Description = "AI pair programming in terminal" },
        new() { Name = "Amazon Q", BinaryName = "q", Description = "AWS AI developer tool" },
        new() { Name = "Antigravity", BinaryName = "antigravity", Description = "Google agentic dev platform" },
        new() { Name = "Claude Code", BinaryName = "claude", Description = "Anthropic CLI for Claude" },
        new() { Name = "Cline", BinaryName = "cline", Description = "Autonomous coding agent" },
        new() { Name = "Codex", BinaryName = "codex", Description = "OpenAI Codex CLI" },
        new() { Name = "Cody", BinaryName = "cody", Description = "Sourcegraph AI assistant" },
        new() { Name = "Continue", BinaryName = "cn", Description = "Headless coding agent" },
        new() { Name = "Copilot CLI", BinaryName = "copilot", Description = "GitHub AI coding agent" },
        new() { Name = "Devin", BinaryName = "devin", Description = "Cognition AI engineer" },
        new() { Name = "Goose", BinaryName = "goose", Description = "Open-source AI agent" },
        new() { Name = "Kilo Code", BinaryName = "kilo", Description = "Agentic engineering platform" },
        new() { Name = "OpenCode", BinaryName = "opencode", Description = "Open-source AI coding agent", VersionArgs = "version" },
        new() { Name = "Pi Agent", BinaryName = "pi", Description = "Minimal BYOK coding agent" },
    ];

    public static List<AiToolInfo> Detect(Dictionary<string, string>? customPaths = null)
    {
        var results = new List<AiToolInfo>();

        foreach (var template in KnownTools)
        {
            var tool = new AiToolInfo
            {
                Name = template.Name,
                Description = template.Description,
                BinaryName = template.BinaryName,
                VersionArgs = template.VersionArgs
            };

            if (customPaths != null
                && customPaths.TryGetValue(template.BinaryName, out var customPath)
                && File.Exists(customPath))
            {
                tool.ExecutablePath = customPath;
                tool.IsInstalled = true;
                tool.IsCustomPath = true;
            }
            else
            {
                tool.ExecutablePath = FindTool(template.BinaryName);
                tool.IsInstalled = tool.ExecutablePath != null;
            }

            results.Add(tool);
        }

        return results;
    }

    public static async Task<string?> TestAsync(AiToolInfo tool)
    {
        if (tool.ExecutablePath == null) return null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var psi = new ProcessStartInfo
            {
                FileName = tool.ExecutablePath,
                Arguments = tool.VersionArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);

            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException) { try { process.Kill(); } catch { } }

            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            return string.IsNullOrEmpty(firstLine) ? null : firstLine;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("AI tool test failed for {0}: {1}", tool.Name, ex.Message);
            return null;
        }
    }

    private static string? FindTool(string binaryName)
    {
        if (OperatingSystem.IsWindows())
        {
            return ShellDetector.FindExecutable(binaryName + ".exe")
                ?? ShellDetector.FindExecutable(binaryName + ".cmd")
                ?? ShellDetector.FindExecutable(binaryName + ".bat")
                ?? ShellDetector.FindExecutable(binaryName);
        }

        return ShellDetector.FindExecutable(binaryName);
    }
}
