using System.Text.Json.Serialization;
using mTiles.Services;

namespace mTiles.Models;

public sealed class SqlServerDiscoverySettings
{
    public bool Enabled { get; set; }
    public bool UseIntegratedSecurity { get; set; } = true;
    public string Username { get; set; } = "";

    [JsonConverter(typeof(ProtectedStringConverter))]
    public string Password { get; set; } = "";
}
