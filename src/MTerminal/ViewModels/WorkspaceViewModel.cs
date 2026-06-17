using System.Collections.ObjectModel;
using System.Text.Json;
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
    private int _noteCount;
    private int _todoCount;
    private int _gitCount;

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
        else
        {
            RootTile = CreateLeaf(TileContentType.Empty, null, "");
        }
    }

    private LeafTileNodeViewModel CreateLeaf(TileContentType type, ObservableObject? content, string tileName)
    {
        return new LeafTileNodeViewModel(type, content!, WorkingDirectory, (t, d) => CreateContent(t, d), AllocateTileName)
        {
            TileName = tileName,
            LayoutChanged = ScheduleSave,
            RootReplaced = newRoot => RootTile = ConfigureRoot(newRoot),
            RootCleared = () => { RootTile = CreateLeaf(TileContentType.Empty, null, ""); ScheduleSave(); }
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
            leaf.RootCleared = () => { RootTile = CreateLeaf(TileContentType.Empty, null, ""); ScheduleSave(); };
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
        TileContentType.Note => $"Note #{++_noteCount}",
        TileContentType.Todo => $"Todo #{++_todoCount}",
        TileContentType.Git => $"Git #{++_gitCount}",
        TileContentType.Empty => "",
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
                    else if (node.ContentType == TileContentType.Note)
                        _noteCount = Math.Max(_noteCount, num);
                    else if (node.ContentType == TileContentType.Todo)
                        _todoCount = Math.Max(_todoCount, num);
                    else if (node.ContentType == TileContentType.Git)
                        _gitCount = Math.Max(_gitCount, num);
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
            TileContentType.Terminal => new TerminalTileViewModel(workingDir, shell, _settingsService),
            TileContentType.Note => CreateNoteContent(workingDir),
            TileContentType.Todo => CreateTodoContent(workingDir),
            TileContentType.Git => new GitTileViewModel(workingDir, _settingsService) { TileSettingsChanged = ScheduleSave },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }


    private NoteTileViewModel CreateNoteContent(string workingDir)
    {
        var notesDir = Path.Combine(workingDir, ".mterminal", "notes");
        var filePath = Path.Combine(notesDir, $"{Guid.NewGuid():N}.md");
        return new NoteTileViewModel(filePath, _settingsService);
    }

    private TodoTileViewModel CreateTodoContent(string workingDir)
    {
        var todosDir = Path.Combine(workingDir, ".mterminal", "todos");
        var filePath = Path.Combine(todosDir, $"{Guid.NewGuid():N}.md");
        return new TodoTileViewModel(filePath, _settingsService);
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
                NoteFilePath = (leaf.Content as NoteTileViewModel)?.FilePath,
                TodoFilePath = (leaf.Content as TodoTileViewModel)?.FilePath,
                Settings = SerializeTileSettings(leaf)
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
            ObservableObject? content = null;
            if (dto.ContentType != TileContentType.Empty)
            {
                if (dto.ContentType == TileContentType.Note && dto.NoteFilePath != null)
                {
                    content = new NoteTileViewModel(dto.NoteFilePath, _settingsService);
                }
                else if (dto.ContentType == TileContentType.Todo && dto.TodoFilePath != null)
                {
                    content = new TodoTileViewModel(dto.TodoFilePath, _settingsService);
                }
                else if (dto.ContentType == TileContentType.Git)
                {
                    var git = new GitTileViewModel(WorkingDirectory, _settingsService);
                    RestoreTileSettings(git, dto.Settings);
                    git.TileSettingsChanged = ScheduleSave;
                    content = git;
                }
                else
                {
                    ShellProfile? shell = null;
                    if (dto.ShellName != null)
                        shell = AvailableShells.FirstOrDefault(s =>
                            s.Name.Equals(dto.ShellName, StringComparison.OrdinalIgnoreCase));
                    content = CreateContent(dto.ContentType, WorkingDirectory, shell);
                }
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

    private static Dictionary<string, object?>? SerializeTileSettings(LeafTileNodeViewModel leaf)
    {
        if (leaf.Content is GitTileViewModel git)
        {
            if (!git.ShowDiffPanel)
                return new Dictionary<string, object?> { ["showDiffPanel"] = git.ShowDiffPanel };
        }
        return null;
    }

    private static void RestoreTileSettings(GitTileViewModel git, Dictionary<string, object?>? settings)
    {
        if (settings == null) return;
        if (settings.TryGetValue("showDiffPanel", out var val) && val is JsonElement el)
            git.ShowDiffPanel = el.GetBoolean();
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
