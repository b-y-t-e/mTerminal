namespace mTiles.Models;

public sealed class WorkspaceDatabaseTileConfig
{
    public bool Enabled { get; set; }
    public List<WorkspaceDatabaseConfig> Databases { get; set; } = [];
}
