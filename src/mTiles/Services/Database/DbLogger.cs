namespace mTiles.Services.Database;

public sealed class DbLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Instance { get; set; }
    public int? StatusCode { get; set; }
    public long? ElapsedMs { get; set; }
    public string? Error { get; set; }
    public string? Sql { get; set; }
    public string? SqlSnippet { get; set; }

    public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Message}";

    public string SqlVerb
    {
        get
        {
            var s = (Sql ?? SqlSnippet)?.TrimStart();
            if (string.IsNullOrEmpty(s)) return "";
            var end = s.IndexOfAny([' ', '(', '\n', '\r', ';']);
            var word = (end > 0 ? s[..end] : s).ToUpperInvariant();
            return word switch
            {
                "SELECT" or "WITH" or "SHOW" or "EXPLAIN" => "SELECT",
                "INSERT" => "INSERT",
                "UPDATE" => "UPDATE",
                "DELETE" => "DELETE",
                "EXEC" or "EXECUTE" or "CALL" => "EXEC",
                _ => word
            };
        }
    }

    public bool IsWriteVerb => SqlVerb is "INSERT" or "UPDATE" or "DELETE" or "EXEC";
    public bool IsError => StatusCode >= 400;
}

public sealed class DbLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    private readonly List<DbLogEntry> _entries = [];
    private const int MaxEntries = 500;

    public event Action<DbLogEntry>? EntryLogged;

    public IReadOnlyList<DbLogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public DbLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void Write(string message, string category = "System")
    {
        var entry = new DbLogEntry
        {
            Timestamp = DateTime.Now,
            Category = category,
            Message = message
        };
        AddEntry(entry);
    }

    public void WriteQuery(string clientIp, string instance, string? sql,
        int statusCode, long elapsedMs, int responseSize, string? error)
    {
        string sqlSnippet = sql != null
            ? (sql.Length <= 80 ? sql : sql[..80] + "...")
            : "-";
        sqlSnippet = sqlSnippet.Replace('\n', ' ').Replace('\r', ' ');

        string line;
        if (error != null)
            line = $"{clientIp} | {instance} | {statusCode} | {elapsedMs}ms | ERR: {error} | SQL: {sqlSnippet}";
        else
            line = $"{clientIp} | {instance} | {statusCode} | {elapsedMs}ms | {responseSize} bytes | SQL: {sqlSnippet}";

        var entry = new DbLogEntry
        {
            Timestamp = DateTime.Now,
            Category = "Http",
            Message = line,
            Instance = instance,
            StatusCode = statusCode,
            ElapsedMs = elapsedMs,
            Error = error,
            Sql = sql,
            SqlSnippet = sqlSnippet
        };
        AddEntry(entry);
    }

    private void AddEntry(DbLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries + 50)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }

        try
        {
            var path = Path.Combine(_logDirectory, $"{entry.Timestamp:yyyy-MM-dd}.log");
            using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            sw.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Failed to write DB log: {ex.Message}");
        }

        EntryLogged?.Invoke(entry);
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }

    public void Dispose()
    {
        lock (_lock) _entries.Clear();
    }
}
