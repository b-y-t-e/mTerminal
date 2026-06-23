namespace mTiles.Models;

public sealed class DatabaseSettings
{
    public bool Enabled { get; set; }
    public int HttpPort { get; set; } = 18090;

    public SqlServerDiscoverySettings SqlServer { get; set; } = new();
    public PostgreSqlDiscoverySettings PostgreSql { get; set; } = new();

    public int DiscoveryIntervalMinutes { get; set; } = 30;
    public int StaleCycles { get; set; } = 3;

    public List<ManualDatabaseConnection> ManualConnections { get; set; } = [];
}
