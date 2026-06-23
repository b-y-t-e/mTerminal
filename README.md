# MTerminal

Cross-platform terminal manager with a tiling panel system. Windows, Linux, macOS.

## Features

- **Workspaces** — each workspace is linked to a directory, one-click switching, displays the current git branch
- **Split panes** — split panels horizontally/vertically in any combination
- **Terminals** — Git Bash, PowerShell, CMD (auto-detection), default shell selection in settings
- **Shell Profiles** — named profiles with shell selection, startup script, filtering by shell/AI tool availability, profile chooser when creating a terminal
- **Tile ID** — persistent tile identifier, available in startup script as `${tileId}`, reset with shell restart
- **Git tile** — change viewer in GitHub Desktop style: diff (unified + side-by-side), commit, stash, push/fetch, tag management, undo last commit, commit message suggestions, commit history with tags and unpushed commit markers, context menu (Show in Explorer, Open, Copy path/hash, Discard, Add tag), auto-refresh on worktree changes
- **Database tile** — per-workspace database configuration, HTTP-to-SQL bridge, auto-discovery of SQL Server/PostgreSQL, manual connections, SQL Guard, claude.local.md generation
- **Terminal themes** — Default Dark, Dracula, Nord, Monokai, Solarized Dark, Catppuccin Mocha
- **Note editor** — AvaloniaEdit with line numbering, auto-save to `.mterminal/notes/`
- **Todo list** — inline-editable checklist, Enter adds an item, checkbox moves it to the bottom, auto-save to `.mterminal/todos/`
- **Keyboard shortcuts** — Ctrl+C/V (copy/paste), Alt+key (ESC sequences), Ctrl+Shift+R (restart shell)
- **Workspace context menu** — right-click to open folder in file explorer (Windows/macOS/Linux), remove workspace
- **Panel renaming** — double-click on the name, auto-numbering (Terminal #1, Note #1, Todo #1)
- **Resizable panel** — workspace panel with adjustable width
- **Crash logging** — exception and trace logging to daily files with automatic retention
- **AI Tools** — auto-detection of 18+ CLI AI coding tools (Claude Code, Cursor, Copilot CLI...), version testing, custom tools
- **Persistence** — layout, workspaces, settings, window state, shell profile

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running

```bash
git clone <repo-url>
cd mterminal
dotnet run --project src/MTerminal
```

## Tech stack

- .NET 10 + Avalonia 12
- Iciclecreek.Avalonia.Terminal (PTY)
- AvaloniaEdit (text editor)
- CommunityToolkit.Mvvm
- MessageBox.Avalonia
- Material.Icons.Avalonia

## License

MIT
