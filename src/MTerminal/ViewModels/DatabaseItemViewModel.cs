using CommunityToolkit.Mvvm.ComponentModel;
using MTerminal.Models;

namespace MTerminal.ViewModels;

public partial class DatabaseItemViewModel : ObservableObject
{
    public string Key { get; }
    public string DisplayName { get; }
    public string Provider { get; }
    public string Source { get; }
    public DbProviderType ProviderType { get; }
    public DbSourceType SourceType { get; }
    public string Server { get; }
    public string Database { get; }
    public string Alias { get; }
    public bool HasAlias { get; }
    public string Label { get; }

    [ObservableProperty] private bool _isInWorkspace;
    [ObservableProperty] private bool _allowModifications;

    private readonly string _searchText;

    public DatabaseItemViewModel(DatabaseInstance info, bool isInWorkspace)
    {
        Key = info.Key;
        DisplayName = info.DisplayName;
        Provider = info.Provider.ToString();
        Source = info.Source.ToString();
        ProviderType = info.Provider;
        SourceType = info.Source;
        Server = info.Server;
        Database = info.Database;
        Alias = info.Alias;
        HasAlias = !string.IsNullOrWhiteSpace(info.Alias);
        Label = HasAlias ? info.Alias : info.DisplayName;
        _isInWorkspace = isInWorkspace;
        var name = HasAlias ? info.Alias : info.Database;
        _searchText = $"{name} {Server} {Provider}".ToLowerInvariant();
    }

    public bool MatchesFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var tokens = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (!_searchText.Contains(token, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
