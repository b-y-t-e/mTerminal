# MTerminal

Multiplatformowy terminal manager — .NET 10 + Avalonia 12.

## Budowanie i uruchamianie

```bash
dotnet build
dotnet run --project src/MTerminal
```

## Struktura

- `src/MTerminal/` — jedyny projekt w solucji
- `Models/` — DTO i modele danych (Workspace, PaneNode, AppSettings, ShellProfile)
- `ViewModels/` — MVVM z CommunityToolkit.Mvvm (source generators)
- `Views/` — Avalonia AXAML + code-behind
- `Services/` — persystencja JSON (workspaces, layout, ustawienia)

## Kluczowe biblioteki

- **Iciclecreek.Avalonia.Terminal** — terminal z wbudowanym PTY (Porta.Pty). Ważne: `BeginReparent()`/`EndReparent()` zapobiega zabijaniu procesu przy przenoszeniu kontrolki w visual tree.
- **AvaloniaEdit** — edytor tekstu. Wymaga `StyleInclude` w App.axaml. Sync tekstu przez `Document.Changed` (nie `TextChanged`).

## Architektura split panes

Rekurencyjne drzewo binarne: `LeafPaneNodeViewModel` (terminal/edytor) lub `SplitPaneNodeViewModel` (H/V + dwoje dzieci). `PaneNodeView` zarządza widokami ręcznie (nie DataTemplate) i wywołuje `SuspendTerminals()`/`ResumeTerminals()` wokół Rebuild żeby zachować live terminale.

## Persystencja

- `%APPDATA%/MTerminal/` (Windows) lub `~/.config/MTerminal/` (Linux)
- `settings.json` — ustawienia aplikacji (fonty, theme, default shell, stan okna)
- `workspaces.json` — lista workspace'ów (id, nazwa, ścieżka)
- `workspaces/{id}.json` — layout paneli per workspace
- Auto-save z debounce

## Nomenklatura

- **Workspace** (nie "project") — katalog roboczy z panelami terminali/edytorów
- ViewModele w `ViewModels/`, widoki w `Views/`
- Brak DI container — ręczne wstrzykiwanie w `App.axaml.cs`
