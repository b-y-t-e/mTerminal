using System.Text.Json.Serialization;
using mTiles.Services;

namespace mTiles.Models;

public sealed class PostgreSqlDiscoverySettings
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "";

    [JsonConverter(typeof(ProtectedStringConverter))]
    public string Password { get; set; } = "";
    public int[] Ports { get; set; } = [5432, 5433, 5434];
}
