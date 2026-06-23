namespace mTiles.Services.Database;

public sealed class SqlGuardProfile
{
    public HashSet<string> BlockedFirstWords { get; }
    public HashSet<string> BlockedAnywhere { get; }
    public HashSet<string> AllowedFirstWords { get; }
    public HashSet<string> AlwaysBlockedFirstWords { get; }
    public HashSet<string> AlwaysBlockedAnywhere { get; }
    public bool SupportsDollarQuoting { get; }

    private SqlGuardProfile(
        HashSet<string> blockedFirstWords,
        HashSet<string> blockedAnywhere,
        HashSet<string> allowedFirstWords,
        HashSet<string> alwaysBlockedFirstWords,
        HashSet<string> alwaysBlockedAnywhere,
        bool supportsDollarQuoting)
    {
        BlockedFirstWords = blockedFirstWords;
        BlockedAnywhere = blockedAnywhere;
        AllowedFirstWords = allowedFirstWords;
        AlwaysBlockedFirstWords = alwaysBlockedFirstWords;
        AlwaysBlockedAnywhere = alwaysBlockedAnywhere;
        SupportsDollarQuoting = supportsDollarQuoting;
    }

    private static readonly HashSet<string> _alwaysBlockedFirst = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "TRUNCATE", "ALTER",
        "GRANT", "REVOKE", "DENY",
        "DBCC", "BACKUP", "RESTORE", "KILL", "RECONFIGURE", "SHUTDOWN"
    };

    private static readonly HashSet<string> _alwaysBlockedAnywhere = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "TRUNCATE", "ALTER",
        "GRANT", "REVOKE",
        "OPENQUERY", "OPENROWSET", "OPENDATASOURCE", "DBLINK_EXEC"
    };

    private static readonly SqlGuardProfile _sqlServer = new(
        blockedFirstWords: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE",
            "EXEC", "EXECUTE", "BULK", "MERGE",
            "WAITFOR", "PREPARE"
        },
        blockedAnywhere: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE",
            "EXEC", "EXECUTE"
        },
        allowedFirstWords: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "WITH", "SET", "BEGIN", "COMMIT", "ROLLBACK",
            "SAVEPOINT",
            "EXPLAIN"
        },
        alwaysBlockedFirstWords: _alwaysBlockedFirst,
        alwaysBlockedAnywhere: _alwaysBlockedAnywhere,
        supportsDollarQuoting: false
    );

    private static readonly SqlGuardProfile _postgreSql = new(
        blockedFirstWords: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE",
            "COPY", "VACUUM", "ANALYZE", "REINDEX", "CLUSTER", "COMMENT", "CALL",
            "DO", "LOCK", "DISCARD", "REFRESH",
            "PREPARE", "EXECUTE"
        },
        blockedAnywhere: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE",
            "DO"
        },
        allowedFirstWords: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "WITH", "SHOW", "SET", "RESET", "BEGIN", "COMMIT", "ROLLBACK",
            "SAVEPOINT", "RELEASE", "LISTEN", "UNLISTEN", "NOTIFY",
            "EXPLAIN"
        },
        alwaysBlockedFirstWords: _alwaysBlockedFirst,
        alwaysBlockedAnywhere: _alwaysBlockedAnywhere,
        supportsDollarQuoting: true
    );

    public static SqlGuardProfile SqlServer() => _sqlServer;
    public static SqlGuardProfile PostgreSql() => _postgreSql;
}
