# MTerminal

Multiplatformowy terminal manager — .NET 10 + Avalonia 12.

## Budowanie i uruchamianie

```bash
dotnet build
dotnet run --project src/MTerminal
```

## Struktura

- `src/MTerminal/` — jedyny projekt w solucji
- `Models/` — DTO i modele danych (Workspace, TileNode, AppSettings, AppDefaults, ShellProfile, UserShellProfile, TerminalTheme, GitFileChange, CommitLogEntry, AiToolInfo, UserAiTool, WorkspaceItemViewModel, DatabaseSettings, DatabaseInstance, WorkspaceDatabaseConfig)
- `ViewModels/` — MVVM z CommunityToolkit.Mvvm (source generators)
- `Views/` — Avalonia AXAML + code-behind
- `Styles/` — design tokens (`AppTheme.axaml`) i globalne style kontrolek (`Controls.axaml`, w tym GridSplitter). Kolory UI wyłącznie przez `DynamicResource`, terminal ANSI colors osobno w `TerminalTheme`
- `Services/` — persystencja JSON (PersistenceService, SettingsService, WorkspaceService), detekcja shelli (ShellDetector), detekcja AI tools (AiToolDetector), ThemeBridge, JsonDefaults, AppPaths, GitService/GitCommandRunner/GitDirectoryWatcher, DiffFormatter, FileHelper, TileFactory, TileTreeSerializer, UpdateService, CrashHandler, FileLogWriter, LogTraceListener
- `Services/Database/` — DatabaseServiceManager, DbHttpServer, DiscoveryService, DbRegistry, DbLogger, QueryHandler, SqlGuard, SqlGuardProfile, SqlServerProvider, PostgreSqlProvider, SubnetScanner, IDbProvider
- `Views/PtyWriter.cs` — statyczny helper do zapisu do PTY przez refleksję (`TerminalView._ptyConnection.WriterStream`). Używany przez `TerminalKeyHandler` i startup script. `AttachStartupScript` podpina ShellReady handler z substitucją `${tileId}`
- `ViewModels/TileActivationScope.cs` — per-workspace scope aktywacji tile'ów z mechanizmem supresji

## Kluczowe biblioteki

- **Iciclecreek.Avalonia.Terminal** — terminal z wbudowanym PTY (Porta.Pty).
  - `BeginReparent()`/`EndReparent()` zapobiega zabijaniu procesu przy przenoszeniu w visual tree
  - `Process = string.Empty` blokuje auto-launch domyślnego shella
  - Class handlery (`OnKeyDown`) ignorują `e.Handled` — nie da się ich zablokować zwykłymi handlerami
- **AvaloniaEdit** — edytor tekstu. Wymaga `StyleInclude` w App.axaml. Sync tekstu przez `Document.Changed`.
- **Material.Icons.Avalonia** — ikony Material Design. Wymaga `<MaterialIconStyles />` w `App.axaml` Styles. Użycie: `<mi:MaterialIcon Kind="Close" />`.

## Architektura split tiles

Rekurencyjne drzewo binarne: `LeafTileNodeViewModel` (terminal/edytor) lub `SplitTileNodeViewModel` (H/V + dwoje dzieci). `TileNodeView` zarządza widokami ręcznie (nie DataTemplate) i wywołuje `SuspendTerminals()`/`ResumeTerminals()` wokół Rebuild żeby zachować live terminale.

`LeafTileNodeViewModel.IsActive` — `TileActivationScope` (instancja per workspace) gwarantuje, że tylko jeden tile jest aktywny. `LeafTileView` reaguje na `IsActive` — kolorowy pasek (`ActiveStrip`, 2px) na górze toolbara + jaśniejsze tło (`BgElevated`).

`TileActivationScope.SuppressActivation()` — guard (IDisposable) blokujący kaskadę GotFocus → Activate podczas programmatycznych Focus() i Rebuild. Używany w `LeafTileView.FocusContent()` i `TileNodeView.Rebuild()`.

## Tile ID

Każdy tile ma persystentny `TileId` (`Guid.NewGuid().ToString()`, format z myślnikami). Generowany przy tworzeniu, zapisywany w `TileNode.TileId` w workspace JSON. Propagowany do `TerminalTileViewModel.TileId`.

Przycisk "Reset ID" (ikona `Identifier`) w headerze tile'a — generuje nowy GUID, restartuje shell (po potwierdzeniu dialogiem). Dostępny tylko dla terminali.

W startup script `${tileId}` jest podmieniane na aktualny `TileId` — zarówno przy pierwszym uruchomieniu jak i przy restarcie.

## Obsługa klawiszy terminala

`TerminalKeyHandler` (osobna klasa, SRP) obsługuje:
- **Ctrl+V** — paste via `PasteAsync()`
- **Ctrl+C** — copy zaznaczonego tekstu (pre-captured w `PointerReleased`, bo TerminalView czyści selekcję przy każdym keydown)
- **Alt+key** — wysyła `ESC+char` bezpośrednio do PTY stream (fix brakującej obsługi Alt w TerminalView)

Refleksja na PTY stream wyekstrahowana do `PtyWriter` (statyczny helper, wspólny dla key handler i startup script).

## Alt-buffer cleanup (TUI apps)

`AttachAltBufferCleanup` w `TerminalTileView` resetuje mouse tracking (refleksja na `XTerm.Terminal._mouseTracker`) gdy `IsAlternateBuffer` zmienia się z `true` na `false`. Bez tego po wyjściu z opencode/vim terminal jest zalewany sekwencjami SGR mouse.

## ThemeBridge — synchronizacja UI z motywem terminala

`ThemeBridge.Apply(TerminalTheme)` w `App.axaml.cs` dynamicznie wyprowadza kolory UI (tła, bordery, tekst, akcenty) z aktywnego motywu terminala. Wywoływany na starcie i przy każdym `SettingsChanged`. Dzięki `DynamicResource` cały UI reaguje natychmiast na zmianę motywu.

## Shell Profiles

Użytkownik definiuje profile shella w Settings → zakładka Profiles. Każdy `UserShellProfile` ma: `Id` (GUID), `Name`, `ShellName` (referencja do wykrytego shella), `StartupScript` (komendy wysyłane do PTY po starcie), `FallbackScript` (uruchamiany gdy StartupScript zfailuje), `RequiredAiToolBinaryName` (opcjonalny — binary name narzędzia AI wymaganego do wyświetlenia profilu).

**Seed domyślnych profili:** `SettingsService.SeedDefaultProfiles()` dodaje 4 profile (Claude Code, OpenCode, Codex, Pi Agent) jeśli nie istnieje profil o takiej nazwie (case-insensitive). Nigdy nie nadpisuje istniejących profili.

**Filtrowanie profili:** Profil jest widoczny na empty tile tylko jeśli:
- `RequiredAiToolBinaryName` jest puste LUB narzędzie AI jest zainstalowane (`AiToolDetector.Detect`)

Filtrowanie realizowane w `WorkspaceViewModel.GetAvailableProfiles()` z cache (30s TTL) na wyniki `AiToolDetector.Detect()`.

**DirectLauncher** (`Views/DirectLauncher.cs`): gdy profil ma `FallbackScript` → `IsDirectLaunch = true`. Komendy uruchamiane przez `shell -c "command"` (nie interaktywnie). Chain: startup → fail (<5s) → fallback → fail → normalny interaktywny shell. Jeśli komenda przeżyje >5s → sukces → auto-relaunch po wyjściu (gdy lifetime >10s). Bez `FallbackScript` → klasyczny tryb: shell startuje interaktywnie, startup script wpisywany przez `PtyWriter`.

Flow tworzenia terminala z profilem:
1. Empty tile → klik Terminal → jeśli są profile, pojawia się ProfileChooser (Back / Default / przyciski profili)
2. Wybór profilu → `TileFactory.CreateContent(..., UserShellProfile)` → `ShellDetector.ResolveFromUserProfile()` → `TerminalTileViewModel` z shellem + startup scriptem
3. `IsDirectLaunch` → `DirectLauncher.LaunchWithFallback()`, else → `PtyWriter.AttachStartupScript()` + `LaunchProcess`

Persystencja profilu w layout: `TileNode.UserProfileId` → przy deserializacji `TileFactory.CreateTerminalFromDto` szuka profilu po Id w `AppSettings.ShellProfiles`. Jeśli profil usunięty — graceful fallback na `ShellName`.

## Settings UI

Dialog Settings jako modal overlay z responsywnym rozmiarem (50% szerokości / 80% wysokości okna, min 420×400). Trzy zakładki:
- **General** — Default Shell, Appearance (theme, color theme, font), Terminal (font)
- **Profiles** — CRUD profili shella (lista + inline edit z akcentowym borderem)
- **AI Tools** — autodetekcja CLI AI coding tools, test wersji, custom tools

`SettingsViewModel.SelectedTab` steruje widocznością zakładek (0=General, 1=Profiles, 2=AI Tools). Style tab-buttons: `settings-tab` / `settings-tab-active` w `Controls.axaml`.

## AI Tools

Zakładka AI Tools w Settings wykrywa zainstalowane CLI AI coding tools i pozwala zarządzać custom tools.

**Modele:** `AiToolInfo` (runtime DTO z detekcji), `UserAiTool` (persystowany custom tool z Id/Name/BinaryName/VersionArgs/CustomPath).

**AiToolDetector** (statyczny, wzorowany na `ShellDetector`):
- `Detect(customPaths, userTools)` — skanuje PATH + znane domowe lokalizacje (`~/.local/bin`, `~/go/bin`, `~/.{tool}/bin`, `%APPDATA%/npm`, `~/.cargo/bin`) z rozszerzeniami `.exe`/`.cmd`/`.bat` na Windows. Custom paths mają priorytet nad auto-detect. User tools mergowane z wbudowaną listą 18 narzędzi.
- `TestAsync(AiToolInfo)` — uruchamia version command z 5s timeout, zwraca pierwszą linię stdout.
- `FindInHomeDirs` — fallback gdy narzędzie nie jest na systemowym PATH (GUI app nie widzi ścieżek z shell profile).

**AiToolViewModel** — MVVM wrapper z niezależnymi komendami per narzędzie (TestCommand, OpenFolderCommand, BrowsePathCommand, OpenUrlCommand, DeleteCommand). `BrowseFile` callback podpięty z View (file picker). `OnCustomPathSet` callback zapisuje do settings.

**Lazy loading:** Detekcja uruchamiana przy pierwszym wejściu na tab AI Tools (`OnSelectedTabChanged`), nie przy starcie aplikacji.

**Sortowanie:** Zainstalowane narzędzia na górze (alfabetycznie), niewykryte pod spodem (alfabetycznie).

**Persystencja w AppSettings:**
- `CustomAiToolPaths` (Dict<string,string>) — nadpisane ścieżki dla wbudowanych narzędzi
- `CustomAiTools` (List<UserAiTool>) — user-defined tools z CRUD w UI

**UI karty narzędzia:** Left status strip (3px, zielony/szary), nazwa + wersja, binary w monospace + ścieżka, badge (CUSTOM/NOT FOUND), przyciski (delete/browse/folder/url/test). "Add Custom Tool" jako `add-row` na końcu listy.

## Database tile

Pasywny tile konfiguracji baz danych per workspace. Nie zarządza serwisem (start/stop/discovery w Settings).

**Tile UI:** Checkbox `Enabled` (kontroluje zapis do `claude.local.md`), lista wybranych baz z toggle RW/RO, lista wszystkich wykrytych baz z przyciskiem dodania. Tile reaguje na `DatabaseServiceManager.StateChanged` i `SettingsChanged`.

**Architektura:** `DatabaseServiceManager` (singleton w App) zarządza `DbRegistry`, `DbLogger`, `DiscoveryService` i `DbHttpServer`. Tile rejestruje swój workspace w managerze (`RegisterWorkspace`/`UnregisterWorkspace`).

**Globalny access control:** HTTP server odpytuje tylko bazy wybrane w co najmniej jednym workspace tile z `Enabled = true`. `IsDatabaseAllowed(key)` sprawdza unię grantów ze wszystkich workspace'ów. Baza usunięta ze wszystkich tile'ów jest automatycznie blokowana. `GET /databases` zwraca tylko dozwolone bazy.

**Detekcja baz:** SQL Server przez UDP broadcast na port 1434 (SQL Browser). PostgreSQL przez skanowanie portów (domyślnie 5432, 5433, 5434) na localhost i w sieci lokalnej. Discovery uruchamiane cyklicznie (domyślnie co 30 min).

**HTTP Server:** `DbHttpServer` na konfigurowalnym porcie (domyślnie 18080). Endpointy:
- `GET /databases` — lista dozwolonych baz (filtrowana przez granty)
- `GET/POST /query/{server}/{database}?sql=...` — zapytania SQL (tylko dozwolone bazy)
- `GET/POST /query/{server}/{instance}/{database}` — z instancją
- Limit body POST: 512KB. Limit wyników: 50k wierszy / 16MB.

**SQL Guard:** Blokada DML (INSERT/UPDATE/DELETE) domyślnie. Odblokowanie per-baza w workspace config. DROP/TRUNCATE/ALTER zawsze zablokowane (nawet z allowModifications). Hasła w settings szyfrowane DPAPI.

**Workspace config:** `.mterminal/databases.json` — `WorkspaceDatabaseTileConfig` z `Enabled` (bool) i `Databases` (lista). Backward compat: stary format (JSON array) → `Enabled = true`. Przy zmianie konfiguracji generowany jest `claude.local.md` (tylko gdy `Enabled = true`).

**Settings:** Zakładka Database w Settings — włączenie usługi, port HTTP, konfiguracja SQL Server (Windows Auth / SQL Auth) i PostgreSQL (credentials, porty), interwał skanowania, manual connections (CRUD z inline edit form, test connection). Save & Apply restartuje serwis automatycznie.

**Manual Connections:** `ManualDatabaseConnection` w `DatabaseSettings.ManualConnections` — ręcznie definiowane połączenia (server, instance, database, port, auth). Rejestrowane w `DbRegistry` z `Source = "Manual"` przy starcie serwisu. Chronione przed nadpisaniem przez discovery (`TryRegister` sprawdza `Source == "Manual"`). Hasła szyfrowane DPAPI (`ProtectedStringConverter`).

**Logi:** `DbLogger` — logi zapytań HTTP i discovery w pamięci (max 500) + pliki dzienne w `%APPDATA%/MTerminal/db-logs/`.

**Services:** `Services/Database/` — IDbProvider, SqlServerProvider, PostgreSqlProvider, SqlGuard, SqlGuardProfile, QueryHandler, DbRegistry, DiscoveryService, DbHttpServer, DbLogger, SubnetScanner, DatabaseServiceManager, ClaudeLocalMdWriter.

## Restart shell

`RestartTerminal` w `LeafTileNodeViewModel` — kill + relaunch PTY. Dostępny przez ikonę Restart w headerze tile'a i Ctrl+Shift+R. Workaround na hang ConPTY po Ctrl+C w TUI apps (znany bug opencode na Windows).

## Scrollbar Fluent theme fix

`AppTheme.axaml` nadpisuje `VerticalSmallScrollThumbScaleTransform` / `HorizontalSmallScrollThumbScaleTransform` na `none`. Bez tego Fluent theme skaluje thumb do 12.5% na maszynach z domyślnym Windows "auto-hide scrollbars".

## Crash handling i logowanie

`CrashHandler` przechwytuje wyjątki z trzech źródeł: `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, `Dispatcher.UIThread.UnhandledException`. Inicjalizowany w `Program.Main()` przed startem Avalonia.

`FileLogWriter` zapisuje logi do `%APPDATA%/MTerminal/logs/mterminal-YYYY-MM-DD.log` z automatycznym czyszczeniem plików starszych niż 7 dni. `LogTraceListener` przekierowuje `Trace` do plików logów.

## Persystencja

- `%APPDATA%/MTerminal/` (Windows) lub `~/.config/MTerminal/` (Linux)
- `settings.json` — ustawienia (fonty, theme terminala, default shell, shell profiles, custom AI tool paths/tools, stan okna)
- `workspaces.json` — lista workspace'ów (id, nazwa, ścieżka)
- `workspaces/{id}.json` — layout tile'ów per workspace (shell name, user profile id, tile id, tile name). Backward compat: `RootPane` → `RootTile` migracja w `WorkspaceState`
- `logs/` — logi aplikacji (dzienne pliki, retencja 7 dni)
- Auto-save z debounce

## Konwencje

- **Workspace** (nie "project") — katalog roboczy z tile'ami terminali/edytorów. PPM na workspace → context menu (Show in Explorer, Remove).
- **Tile** (nie "pane"/"panel") — pojedynczy kafelek w workspace (terminal, notatka lub todo), dzielony w drzewo binarne
- **Note** (nie "editor") — tile z edytorem tekstu (AvaloniaEdit), TileContentType.Note
- **Todo** — tile z listą zadań, TileContentType.Todo
- ViewModele w `ViewModels/`, widoki w `Views/`
- **Git** — tile z podglądem zmian (diff, commit, stash, push, fetch, tags, undo, context menu, discard), TileContentType.Git
- **Database** — tile z zarządzaniem bazami danych (SQL Server, PostgreSQL), HTTP bridge, logi zapytań, TileContentType.Database
- Brak DI container — ręczne wstrzykiwanie w `App.axaml.cs`, `TileFactory` jako fabryka contentu tile'ów
- **ConfirmAction pattern** — destructive actions (discard, remove workspace, undo commit) używają `Func<string, Task<bool>>? ConfirmAction` w ViewModel, podpięty z View jako `MessageBox.Avalonia` dialog (YesNo)
- **PromptInput pattern** — `Func<string, string, IEnumerable<string>?, Task<string?>>? PromptInput` w ViewModel, podpięty z View jako `InputDialog` (title + text input + suggestions list). Używany np. przy tworzeniu taga.
- **ShowError pattern** — `Func<string, string, Task>? ShowError` w ViewModel, podpięty z View jako `MessageBox.Avalonia` (Ok). Używany przy błędach push/fetch/tag/undo.

## Git tile — szczegóły

`GitDirectoryWatcher` obserwuje zarówno `.git/` jak i cały katalog roboczy (worktree). Lista ignorowanych katalogów pobierana z `git ls-files --ignored` i aktualizowana przy każdym refresh. Handlery `Error` na watcherach logują przepełnienie bufora i triggerują refresh.

`ReconcileChanges` w `GitTileViewModel` zachowuje stan checkboxów (`IsChecked`) między refresh'ami na podstawie klucza (FilePath + Status + mtime). Dwupoziomowy cache (currentState + previousState) chroni przed utratą stanu przy "migających" plikach. Przy pierwszym ładowaniu checkboxy = false, przy kolejnych refresh'ach nowe/zmienione pliki = true.

Context menu (PPM) na liście plików: Show in Explorer, Open in default program, Copy filename/folder/filepath, Discard changes (z dialogiem potwierdzenia). Multi-select: PPM pokazuje tylko Discard z liczbą plików. Space toggle'uje checkboxy zaznaczonych plików.

Context menu (PPM) na liście commitów: Add tag..., Copy commit hash.

**Push/Fetch/Undo:** Przyciski w tab barze Git tile. Push wykrywa upstream (brak → `push -u origin`). Fetch robi `fetch --all --prune`. Undo = `reset --soft HEAD~1`, dostępny tylko gdy ostatni commit jest lokalny (niepushowany). Wszystkie z error dialog.

**Tags:** Wyświetlane w historii commitów (kolor `TagColor`). Tworzenie przez context menu → `InputDialog` z listą ostatnich tagów. Walidacja nazwy regexem `[a-zA-Z0-9._/\-]+`.

**Unpushed commits:** Oznaczone `*` (kolor `DangerText`) w historii. Licznik `(N)` przy przycisku Push. Logika: `git log upstream..HEAD`.

**Commit suggestions:** Popup przy polu commit message (ikona zegara). Top-3 najczęstsze + 10 ostatnich unikalnych z `git log --format=%s -50`.

**`.mterminal/` filtering:** Setting `GitHideMTerminalDir` (default true) ukrywa pliki `.mterminal/` w liście zmian Git tile.

**DiffFontSize:** Panel diff używa 80% rozmiaru fontu (`FontSize * 0.8`).

## Workspace view caching

`MainWindow` cachuje `WorkspaceView` instancje w `Dictionary<string, WorkspaceView>`. Przełączanie workspace'ów przez `IsVisible` toggle zamiast DataTemplate — terminale nie są zabijane/odtwarzane. `WorkspaceRemoved` event czyści cache i usuwa widok z visual tree.

## Workspace panel — branch names

`WorkspaceItemViewModel` — wrapper na `Workspace` z `ObservableProperty BranchName`. Panel workspace'ów wyświetla branch name obok ścieżki (ikona SourceBranch + nazwa). `DispatcherTimer` co 30s odpytuje `GitService.GetBranchNameAsync` (statyczna metoda, tworzy tymczasowy `GitCommandRunner`). Dispose w `MainWindowViewModel.OnClosing`.

## InputDialog

Reusable modal dialog (`Views/InputDialog.axaml`): tytuł, TextBox z placeholder, opcjonalna lista sugestii (ListBox). Enter = OK, Escape = Cancel. Klik na sugestię wpisuje ją do TextBoxa. `ShowDialog<string?>` zwraca trimmed text lub null.
