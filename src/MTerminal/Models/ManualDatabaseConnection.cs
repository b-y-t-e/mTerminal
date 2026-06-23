using System.Text.Json.Serialization;
using MTerminal.Services;

namespace MTerminal.Models;

public sealed class ManualDatabaseConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DbProviderType Provider { get; set; } = DbProviderType.SqlServer;
    public string Alias { get; set; } = "";
    public string Server { get; set; } = "";
    public string Instance { get; set; } = "";
    public string Database { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";

    [JsonConverter(typeof(ProtectedStringConverter))]
    public string Password { get; set; } = "";
    public bool UseIntegratedSecurity { get; set; }
}
