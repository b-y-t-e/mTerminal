using System.Collections.Concurrent;
using mTerminal.Models;

namespace mTerminal.Services.Database;

public sealed class DbRegistry
{
    private readonly ConcurrentDictionary<string, RegistryEntry> _instances = new(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public sealed class RegistryEntry
    {
        public DatabaseInstance Info { get; }
        public IDbProvider? Provider { get; }
        public QueryHandler? Handler { get; }

        public RegistryEntry(DatabaseInstance info, IDbProvider? provider)
        {
            Info = info;
            Provider = provider;
            Handler = provider != null ? new QueryHandler(provider) : null;
        }
    }

    public IEnumerable<RegistryEntry> Entries => _instances.Values.Distinct();
    public int Count => _instances.Values.Distinct().Count();

    public bool TryGet(string key, out RegistryEntry? entry) =>
        _instances.TryGetValue(key, out entry);

    public bool TryRegister(DatabaseInstance info)
    {
        if (_instances.TryGetValue(info.Key, out var existing))
        {
            if (existing.Info.Source == DbSourceType.Manual) return false;
            existing.Info.LastSeen = DateTime.UtcNow;
            if (existing.Info.Source == info.Source) return false;
        }

        var provider = CreateProvider(info.Provider, info.ConnectionString);
        if (provider == null) return false;

        var entry = new RegistryEntry(info, provider);
        _instances[info.Key] = entry;
        Changed?.Invoke();
        return true;
    }

    public void Register(DatabaseInstance info)
    {
        var provider = CreateProvider(info.Provider, info.ConnectionString);
        var entry = new RegistryEntry(info, provider);
        _instances[info.Key] = entry;
        if (!string.IsNullOrWhiteSpace(info.Alias))
            _instances[info.Alias.ToLowerInvariant()] = entry;
        Changed?.Invoke();
    }

    public bool Remove(string key)
    {
        if (_instances.TryRemove(key, out var entry))
        {
            var alias = entry.Info.Alias;
            if (!string.IsNullOrWhiteSpace(alias))
                _instances.TryRemove(alias.ToLowerInvariant(), out _);
            Changed?.Invoke();
            return true;
        }
        return false;
    }

    public int RemoveStale(TimeSpan maxAge)
    {
        int removed = 0;
        var cutoff = DateTime.UtcNow - maxAge;
        var toRemove = new List<string>();
        foreach (var kvp in _instances)
        {
            if (kvp.Value.Info.Source != DbSourceType.Discovered) continue;
            if (kvp.Value.Info.LastSeen < cutoff)
                toRemove.Add(kvp.Value.Info.Key);
        }
        foreach (var key in toRemove.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_instances.TryRemove(key, out var entry))
            {
                var alias = entry.Info.Alias;
                if (!string.IsNullOrWhiteSpace(alias))
                    _instances.TryRemove(alias.ToLowerInvariant(), out _);
                removed++;
            }
        }
        if (removed > 0) Changed?.Invoke();
        return removed;
    }

    public static IDbProvider? CreateProvider(DbProviderType provider, string connectionString)
    {
        try
        {
            return provider switch
            {
                DbProviderType.SqlServer => new SqlServerProvider(connectionString),
                DbProviderType.PostgreSQL => new PostgreSqlProvider(connectionString),
                _ => null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Failed to create DB provider ({provider}): {ex.Message}");
            return null;
        }
    }
}
