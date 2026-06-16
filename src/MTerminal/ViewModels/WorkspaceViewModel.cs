using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class WorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly PersistenceService _persistenceService;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private TileNodeViewModel? _rootTile;

    public string WorkspaceId { get; }
    public string WorkingDirectory { get; }
    public ObservableCollection<ShellProfile> AvailableShells { get; } = [];

    private int _terminalCount;
    private int _editorCount;

    public WorkspaceViewModel(Workspace workspace, PersistenceService persistenceService, SettingsService settingsService)
    {
        WorkspaceId = workspace.Id;
        WorkingDirectory = workspace.DirectoryPath;
        _persistenceService = persistenceService;
        _settingsService = settingsService;

        foreach (var shell in ShellDetector.Detect())
            AvailableShells.Add(shell);

        var state = persistenceService.LoadLayout(workspace.Id);
        if (state?.RootTile != null)
        {
            InitCountersFromDto(state.RootTile);
            RootTile = RestoreTree(state.RootTile);
        }
    }

    [RelayCommand]
    private void AddTerminal(ShellProfile? shell = null) => AddFirstTile(TileContentType.Terminal, shell);

    [RelayCommand]
    private void AddEditor() => AddFirstTile(TileContentType.TextEditor);

    private void AddFirstTile(TileContentType type, ShellProfile? shell = null)
    {
        if (RootTile != null) return;
        var content = CreateContent(type, WorkingDirectory, shell);
        RootTile = CreateLeaf(type, content, AllocateTileName(type));
        ScheduleSave();
    }

    private LeafTileNodeViewModel CreateLeaf(TileContentType type, ObservableObject content, string tileName)
    {
        return new LeafTileNodeViewModel(type, content, WorkingDirectory, (t, d) => CreateContent(t, d), AllocateTileName)
        {
            TileName = tileName,
            LayoutChanged = ScheduleSave,
            RootReplaced = newRoot => RootTile = ConfigureRoot(newRoot),
            RootCleared = () => { RootTile = null; ScheduleSave(); }
        };
    }

    private TileNodeViewModel ConfigureRoot(TileNodeViewModel node)
    {
        node.LayoutChanged = ScheduleSave;
        PropagateCallbacks(node);
        ScheduleSave();
        return node;
    }

    private void PropagateCallbacks(TileNodeViewModel node)
    {
        node.LayoutChanged = ScheduleSave;
        if (node is LeafTileNodeViewModel leaf)
        {
            leaf.RootReplaced = newRoot => RootTile = ConfigureRoot(newRoot);
            leaf.RootCleared = () => { RootTile = null; ScheduleSave(); };
        }
        else if (node is SplitTileNodeViewModel split)
        {
            if (split.First != null) PropagateCallbacks(split.First);
            if (split.Second != null) PropagateCallbacks(split.Second);
        }
    }

    private string AllocateTileName(TileContentType type) => type switch
    {
        TileContentType.Terminal => $"Terminal #{++_terminalCount}",
        TileContentType.TextEditor => $"Note #{++_editorCount}",
        _ => type.ToString()
    };

    private static readonly System.Text.RegularExpressions.Regex TileNumberRegex = new(@"#(\d+)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private void InitCountersFromDto(TileNode? node)
    {
        if (node == null) return;
        if (node.IsLeaf)
        {
            if (node.TileName != null)
            {
                var match = TileNumberRegex.Match(node.TileName);
                if (match.Success)
                {
                    var num = int.Parse(match.Groups[1].Value);
                    if (node.ContentType == TileContentType.Terminal)
                        _terminalCount = Math.Max(_terminalCount, num);
                    else if (node.ContentType == TileContentType.TextEditor)
                        _editorCount = Math.Max(_editorCount, num);
                }
            }
        }
        else
        {
            InitCountersFromDto(node.First);
            InitCountersFromDto(node.Second);
        }
    }

    private ObservableObject CreateContent(TileContentType type, string workingDir, ShellProfile? shell = null)
    {
        return type switch
        {
            TileContentType.Terminal => new TerminalTileViewModel(workingDir, shell, _settingsService.Settings),
            TileContentType.TextEditor => CreateEditorContent(workingDir),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private EditorTileViewModel CreateEditorContent(string workingDir)
    {
        var s = _settingsService.Settings;
        var notesDir = Path.Combine(workingDir, ".mterminal", "notes");
        var filePath = Path.Combine(notesDir, $"{Guid.NewGuid():N}.md");
        return new EditorTileViewModel(filePath, s.EditorFontFamily, s.EditorFontSize);
    }

    private void ScheduleSave()
    {
        _persistenceService.DebouncedSaveLayout(WorkspaceId, () => SerializeTree(RootTile));
    }

    private TileNode? SerializeTree(TileNodeViewModel? vm)
    {
        return vm switch
        {
            LeafTileNodeViewModel leaf => new TileNode
            {
                IsLeaf = true,
                ContentType = leaf.ContentType,
                TileName = leaf.TileName,
                ShellName = (leaf.Content as TerminalTileViewModel)?.Shell.Name,
                EditorFilePath = (leaf.Content as EditorTileViewModel)?.FilePath
            },
            SplitTileNodeViewModel split => new TileNode
            {
                IsLeaf = false,
                SplitOrientation = split.Orientation,
                SplitRatio = split.SplitRatio,
                First = SerializeTree(split.First),
                Second = SerializeTree(split.Second)
            },
            _ => null
        };
    }

    private TileNodeViewModel? RestoreTree(TileNode dto)
    {
        if (dto.IsLeaf)
        {
            ObservableObject content;
            if (dto.ContentType == TileContentType.TextEditor && dto.EditorFilePath != null)
            {
                var s = _settingsService.Settings;
                content = new EditorTileViewModel(dto.EditorFilePath, s.EditorFontFamily, s.EditorFontSize);
            }
            else
            {
                ShellProfile? shell = null;
                if (dto.ShellName != null)
                    shell = AvailableShells.FirstOrDefault(s =>
                        s.Name.Equals(dto.ShellName, StringComparison.OrdinalIgnoreCase));
                content = CreateContent(dto.ContentType, WorkingDirectory, shell);
            }

            return CreateLeaf(dto.ContentType, content, dto.TileName ?? AllocateTileName(dto.ContentType));
        }

        var first = RestoreTree(dto.First!);
        var second = RestoreTree(dto.Second!);
        if (first == null || second == null) return first ?? second;

        var split = new SplitTileNodeViewModel(dto.SplitOrientation, first, second)
        {
            SplitRatio = dto.SplitRatio,
            LayoutChanged = ScheduleSave
        };
        first.Parent = split;
        second.Parent = split;
        return split;
    }

    public void Dispose()
    {
        DisposeTree(RootTile);
    }

    private static void DisposeTree(TileNodeViewModel? node)
    {
        if (node is LeafTileNodeViewModel leaf && leaf.Content is IDisposable d)
            d.Dispose();
        else if (node is SplitTileNodeViewModel split)
        {
            DisposeTree(split.First);
            DisposeTree(split.Second);
        }
    }
}
