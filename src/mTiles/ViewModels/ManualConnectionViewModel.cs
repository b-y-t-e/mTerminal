using CommunityToolkit.Mvvm.ComponentModel;
using mTiles.Models;

namespace mTiles.ViewModels;

public partial class ManualConnectionViewModel : ObservableObject
{
    public string Id { get; }
    public DbProviderType Provider { get; }
    public string Alias { get; }
    public string Server { get; }
    public string Instance { get; }
    public string Database { get; }
    public int Port { get; }
    public bool UseIntegratedSecurity { get; }

    [ObservableProperty] private bool _isTesting;
    [ObservableProperty] private string? _testResult;

    public string DisplayServer
    {
        get
        {
            var s = Server;
            if (!string.IsNullOrEmpty(Instance)) s += $"\\{Instance}";
            if (Port > 0) s += $":{Port}";
            return s;
        }
    }

    public string ProviderShort => Provider == DbProviderType.PostgreSQL ? "PG" : "SQL";
    public bool HasAlias => !string.IsNullOrWhiteSpace(Alias);
    public string Label => HasAlias ? Alias : Database;

    public ManualConnectionViewModel(ManualDatabaseConnection mc)
    {
        Id = mc.Id;
        Provider = mc.Provider;
        Alias = mc.Alias;
        Server = mc.Server;
        Instance = mc.Instance;
        Database = mc.Database;
        Port = mc.Port;
        UseIntegratedSecurity = mc.UseIntegratedSecurity;
    }
}
