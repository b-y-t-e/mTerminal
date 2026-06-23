using System.Text;
using mTerminal.Models;

namespace mTerminal.Services.Database;

public static class ClaudeLocalMdWriter
{
    public static void Update(string workspaceDir, IReadOnlyList<WorkspaceDatabaseConfig> databases,
        DbRegistry registry, int httpPort)
    {
        var claudeMdPath = Path.Combine(workspaceDir, "claude.local.md");

        if (databases.Count == 0)
        {
            if (File.Exists(claudeMdPath))
            {
                var content = File.ReadAllText(claudeMdPath);
                var cleaned = RemoveDatabaseSection(content);
                if (string.IsNullOrWhiteSpace(cleaned))
                    File.Delete(claudeMdPath);
                else
                    File.WriteAllText(claudeMdPath, cleaned);
            }
            return;
        }

        var resolvedDbs = new List<(WorkspaceDatabaseConfig Config, DatabaseInstance Info)>();
        foreach (var dbConfig in databases)
        {
            if (registry.TryGet(dbConfig.DatabaseKey, out var entry) && entry != null)
                resolvedDbs.Add((dbConfig, entry.Info));
        }

        if (resolvedDbs.Count == 0)
            return;

        var baseUrl = $"http://localhost:{httpPort}";
        var sb = new StringBuilder();
        sb.AppendLine("# Database access");
        sb.AppendLine();
        sb.AppendLine("SQL queries via local HTTP bridge. SELECT always allowed. DROP/TRUNCATE/ALTER always blocked.");
        sb.AppendLine();

        foreach (var (config, info) in resolvedDbs)
        {
            var urlPath = !string.IsNullOrWhiteSpace(info.Alias)
                ? info.Alias
                : string.Join("/", config.DatabaseKey.Split('/'));
            var rw = config.AllowModifications ? "read-write" : "read-only";
            sb.AppendLine($"- **{info.DisplayName}** ({info.Provider}, {rw}): `{baseUrl}/query/{urlPath}?sql=...` or POST sql body");
        }

        string dbSection = sb.ToString();

        if (File.Exists(claudeMdPath))
        {
            var existing = File.ReadAllText(claudeMdPath);
            var cleaned = RemoveDatabaseSection(existing).TrimEnd();
            File.WriteAllText(claudeMdPath, cleaned.Length == 0
                ? dbSection
                : cleaned + "\n\n" + dbSection);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(claudeMdPath)!);
            File.WriteAllText(claudeMdPath, dbSection);
        }
    }

    private static readonly string[] KnownMarkers =
    [
        "# Database access",
        "# Database Service",
        "# List databases",
    ];

    private static string RemoveDatabaseSection(string content)
    {
        foreach (var marker in KnownMarkers)
        {
            int start = content.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) continue;

            while (start > 0 && content[start - 1] is '\r' or '\n')
                start--;

            int end = content.IndexOf("\n# ", start + marker.Length, StringComparison.Ordinal);
            if (end < 0)
                content = content[..start].TrimEnd();
            else
            {
                end++;
                var before = content[..start].TrimEnd();
                var after = content[end..];
                content = before.Length == 0 ? after : before + "\n\n" + after;
            }
        }

        return content;
    }
}
