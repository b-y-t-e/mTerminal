using System.Text.Json.Serialization;

namespace MTerminal.Models;

public sealed class DatabaseInstance
{
    public string Server { get; set; } = "";
    public string Instance { get; set; } = "";
    public string Database { get; set; } = "";
    public string Alias { get; set; } = "";
    public DbProviderType Provider { get; set; }

    [JsonIgnore]
    public string ConnectionString { get; set; } = "";
    public bool AllowModifications { get; set; }
    public DbSourceType Source { get; set; } = DbSourceType.Discovered;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; set; }

    public string ProviderName => Provider.ToString();

    public string Key => string.IsNullOrEmpty(Instance)
        ? $"{Server}/{Database}".ToLowerInvariant()
        : $"{Server}/{Instance}/{Database}".ToLowerInvariant();

    public string DisplayName => string.IsNullOrEmpty(Instance)
        ? $"{Server}/{Database}"
        : $"{Server}/{Instance}/{Database}";
}
