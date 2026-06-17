using Avalonia.Layout;
using MTerminal.Models;
using MTerminal.ViewModels;

namespace MTerminal.Services;

public sealed class TileTreeSerializer
{
    private readonly TileFactory _tileFactory;
    private readonly IReadOnlyList<ShellProfile> _availableShells;
    private readonly string _workingDirectory;
    private readonly Func<TileContentType, string> _nameAllocator;
    private readonly Action<LeafTileNodeViewModel> _configureLeaf;

    public TileTreeSerializer(
        TileFactory tileFactory,
        SettingsService settingsService,
        IReadOnlyList<ShellProfile> availableShells,
        string workingDirectory,
        Func<TileContentType, string> nameAllocator,
        Action<LeafTileNodeViewModel> configureLeaf)
    {
        _tileFactory = tileFactory;
        _availableShells = availableShells;
        _workingDirectory = workingDirectory;
        _nameAllocator = nameAllocator;
        _configureLeaf = configureLeaf;
    }

    public TileNode? Serialize(TileNodeViewModel? vm)
    {
        return vm switch
        {
            LeafTileNodeViewModel leaf => new TileNode
            {
                IsLeaf = true,
                ContentType = leaf.ContentType,
                TileName = leaf.TileName,
                ShellName = (leaf.Content as TerminalTileViewModel)?.Shell.Name,
                NoteFilePath = (leaf.Content as NoteTileViewModel)?.FilePath,
                TodoFilePath = (leaf.Content as TodoTileViewModel)?.FilePath,
                Settings = TileFactory.SerializeSettings(leaf)
            },
            SplitTileNodeViewModel split => new TileNode
            {
                IsLeaf = false,
                SplitOrientation = split.Orientation,
                SplitRatio = split.SplitRatio,
                First = Serialize(split.First),
                Second = Serialize(split.Second)
            },
            _ => null
        };
    }

    public TileNodeViewModel? Deserialize(TileNode dto, Action scheduleSave)
    {
        if (dto.IsLeaf)
        {
            var content = _tileFactory.CreateFromDto(dto, _workingDirectory, _availableShells, scheduleSave);

            var tileName = dto.TileName ?? _nameAllocator(dto.ContentType);
            var leaf = new LeafTileNodeViewModel(dto.ContentType, content, _workingDirectory,
                (t, d) => _tileFactory.CreateContent(t, d), _nameAllocator)
            {
                TileName = tileName,
                LayoutChanged = scheduleSave
            };
            _configureLeaf(leaf);
            return leaf;
        }

        var first = Deserialize(dto.First!, scheduleSave);
        var second = Deserialize(dto.Second!, scheduleSave);
        if (first == null || second == null) return first ?? second;

        var split = new SplitTileNodeViewModel(dto.SplitOrientation, first, second)
        {
            SplitRatio = dto.SplitRatio,
            LayoutChanged = scheduleSave
        };
        first.Parent = split;
        second.Parent = split;
        return split;
    }
}
