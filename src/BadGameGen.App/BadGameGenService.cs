using System.Text.Json;
using BadGameGen.Core.Content;
using BadGameGen.Core.Export;
using BadGameGen.Core.Generation;
using BadGameGen.Core.Vault;

namespace BadGameGen.App;

public sealed class PackInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    /// <summary>Built-in pack whose library copy the user has edited.</summary>
    public bool Edited { get; set; }
    /// <summary>Edited built-in pack for which the app ships a newer version.</summary>
    public bool UpdateAvailable { get; set; }
    public string FilePath { get; set; } = "";
    public bool Enabled { get; set; }
    public bool Usable { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
    public int TemplateCount { get; set; }
}

public sealed record CategoryDto(string Key, string DisplayName, string Icon, string Description);
public sealed record OptionDto(string Id, string Label, string PackId, List<string> Tags, double Weirdness, double Silliness);

public sealed class BootstrapInfo
{
    public string AppVersion { get; set; } = "";
    public string GeneratorVersion { get; set; } = "";
    public List<CategoryDto> Categories { get; set; } = new();
    public List<string> ScopeNames { get; set; } = new();
    public List<PackInfo> Packs { get; set; } = new();
    public List<PackRef> LibrarySignature { get; set; } = new();
    public List<ThemeDef> Themes { get; set; } = new();
    public List<string> ThemeErrors { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
    public string DataDir { get; set; } = "";
    public string LibraryDir { get; set; } = "";
    public List<string> RemovedBuiltIns { get; set; } = new();
    /// <summary>True when the host can open folders in the OS file manager (desktop app).</summary>
    public bool CanOpenFolders { get; set; }
    public string? VaultNotice { get; set; }
}

public sealed class StatsDto
{
    public int TotalGenerated { get; set; }
    public int VaultCount { get; set; }
    public int FavoriteCount { get; set; }
    public int LibraryItems { get; set; }
    public Dictionary<string, int> ItemsPerCategory { get; set; } = new();
    public List<KeyValuePair<string, int>> TopGenres { get; set; } = new();
    public List<KeyValuePair<string, int>> TopSettings { get; set; } = new();
    public double AverageChaos { get; set; }
    public int CreepAccepted { get; set; }
}

public sealed record ExportResult(string FileName, string Mime, string Content);
public sealed record ImportResult(bool Ok, string? PackId, List<string> Errors, List<string> Warnings);
public sealed record LibraryCheck(bool Changed, BootstrapInfo? Bootstrap, List<string> Broken);

/// <summary>
/// Application services shared by the desktop shell, the dev server and tests.
/// Owns the pack catalog, the active library, the vault and settings.
/// </summary>
public sealed class BadGameGenService
{
    public static readonly string AppVersion =
        typeof(BadGameGenService).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    private readonly object _gate = new();
    private readonly AppPaths _paths;
    private PackCatalog _catalog = new([]);
    private ContentLibrary _library = new([]);
    private Generator _generator = new(new ContentLibrary([]));
    private List<ThemeDef> _themes = new();
    private List<string> _themeErrors = new();
    private readonly Library _packLibrary;
    private string _fingerprint = "";

    /// <summary>Set by the desktop shell to open a folder (or select a file) in Explorer.</summary>
    public Action<string>? OpenInShell { get; set; }

    public SettingsStore Settings { get; }
    public VaultStore Vault { get; }

    public BadGameGenService(AppPaths paths)
    {
        _paths = paths;
        Directory.CreateDirectory(paths.DataDir);
        Directory.CreateDirectory(paths.UserThemesDir);
        Settings = new SettingsStore(paths.SettingsPath);
        Vault = new VaultStore(paths.VaultPath);
        _packLibrary = new Library(paths.BuiltInPacksDir, paths.LibraryDir);
        _packLibrary.Sync();
        Reload();
    }

    /// <summary>Re-reads packs and themes from disk and rebuilds the active library.</summary>
    public void Reload()
    {
        lock (_gate)
        {
            // Everything comes from the editable library folder; shipped packs only seed it.
            var loaded = PackCatalog.Load([(_paths.LibraryDir, false)]);
            _catalog = new PackCatalog(loaded.Results.Select(r => new PackLoadResult
            {
                Pack = r.Pack, Source = r.Source, IsBuiltIn = _packLibrary.IsBuiltInFile(Path.GetFileName(r.Source)),
            }.WithMessages(r)).ToList());
            _fingerprint = Library.Fingerprint(_paths.LibraryDir, _paths.UserThemesDir);
            (_themes, _themeErrors) = ThemeCatalog.Load([(_paths.BuiltInThemesDir, true), (_paths.UserThemesDir, false)]);
            RebuildLibrary();
        }
    }

    private void RebuildLibrary()
    {
        var disabled = new HashSet<string>(Settings.Current.DisabledPacks);
        _library = new ContentLibrary(_catalog.UsablePacks.Where(p => !disabled.Contains(p.Id)));
        _generator = new Generator(_library);
    }

    public BootstrapInfo Bootstrap()
    {
        lock (_gate)
        {
            return new BootstrapInfo
            {
                AppVersion = AppVersion,
                GeneratorVersion = Generator.Version,
                Categories = Categories.All.Select(c => new CategoryDto(c.Key, c.DisplayName, c.Icon, c.Description)).ToList(),
                ScopeNames = Scopes.Names.ToList(),
                Packs = PackInfos(),
                LibrarySignature = _library.Signature.ToList(),
                Themes = _themes,
                ThemeErrors = _themeErrors,
                Settings = Settings.Current,
                DataDir = _paths.DataDir,
                LibraryDir = _paths.LibraryDir,
                RemovedBuiltIns = _packLibrary.RemovedBuiltIns(),
                CanOpenFolders = OpenInShell != null,
                VaultNotice = Vault.RecoveryNotice,
            };
        }
    }

    public List<PackInfo> PackInfos()
    {
        lock (_gate)
        {
            var disabled = new HashSet<string>(Settings.Current.DisabledPacks);
            return _catalog.Results.Select(r => new PackInfo
            {
                Edited = _packLibrary.State(Path.GetFileName(r.Source))?.Edited ?? false,
                UpdateAvailable = _packLibrary.State(Path.GetFileName(r.Source))?.UpdateAvailable ?? false,
                FilePath = r.Source,
                Id = r.Pack?.Id ?? Path.GetFileNameWithoutExtension(r.Source),
                Name = string.IsNullOrWhiteSpace(r.Pack?.Name) ? Path.GetFileName(r.Source) : r.Pack!.Name,
                Version = r.Pack?.Version ?? "",
                Author = r.Pack?.Author ?? "",
                Description = r.Pack?.Description ?? "",
                FileName = Path.GetFileName(r.Source),
                IsBuiltIn = r.IsBuiltIn,
                Usable = r.IsUsable,
                Enabled = r.IsUsable && !disabled.Contains(r.Pack!.Id),
                Errors = r.Errors.Take(50).ToList(),
                Warnings = r.Warnings.Take(50).ToList(),
                Counts = r.Pack?.Items.ToDictionary(kv => kv.Key, kv => kv.Value?.Count ?? 0) ?? new(),
                TemplateCount = r.Pack?.Templates.Sum(kv => kv.Value?.Count ?? 0) ?? 0,
            }).ToList();
        }
    }

    public Concept Generate(Recipe recipe)
    {
        Concept concept;
        lock (_gate) concept = _generator.Generate(recipe);
        Settings.Update(s =>
        {
            s.LastRecipe = concept.Recipe;
            s.TotalGenerated++;
        });
        return concept;
    }

    public List<OptionDto> Options(string category, string? query)
    {
        lock (_gate)
        {
            var q = query?.Trim();
            return _library.Items(category)
                .Where(i => string.IsNullOrEmpty(q)
                            || i.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || i.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .Select(i => new OptionDto(i.FullId, i.Label, i.PackId, i.Tags, i.Weirdness, i.Silliness))
                .ToList();
        }
    }

    /// <summary>Full contents of one pack, for the pack inspector.</summary>
    public ContentPack? PackContents(string id)
    {
        lock (_gate) return _catalog.Results.FirstOrDefault(r => r.Pack?.Id == id)?.Pack;
    }

    public List<PackInfo> SetPackEnabled(string id, bool enabled)
    {
        Settings.Update(s =>
        {
            s.DisabledPacks.Remove(id);
            if (!enabled) s.DisabledPacks.Add(id);
        });
        lock (_gate) RebuildLibrary();
        return PackInfos();
    }

    public ImportResult ImportPack(string fileName, string json)
    {
        var result = PackLoader.LoadJson(json, fileName);
        if (!result.IsUsable)
            return new ImportResult(false, result.Pack?.Id, result.Errors, result.Warnings);
        var id = result.Pack!.Id;
        lock (_gate)
        {
            var existing = _catalog.Results.FirstOrDefault(r => r.Pack?.Id == id);
            if (existing is { IsBuiltIn: true })
                return new ImportResult(false, id, [$"'{id}' is a built-in pack id. Give your pack a different id."], result.Warnings);
            File.WriteAllText(Path.Combine(_paths.LibraryDir, id + ".json"), json);
        }
        Reload();
        return new ImportResult(true, id, [], result.Warnings);
    }

    /// <summary>Removes a pack file from the library. Built-ins can be restored later.</summary>
    public bool RemovePack(string id)
    {
        lock (_gate)
        {
            var existing = _catalog.Results.FirstOrDefault(r => r.Pack?.Id == id || Path.GetFileNameWithoutExtension(r.Source) == id);
            if (existing == null) return false;
            _packLibrary.Remove(Path.GetFileName(existing.Source));
        }
        Reload();
        return true;
    }

    /// <summary>Discards edits to a built-in pack and takes the shipped version.</summary>
    public void ResetPack(string id)
    {
        lock (_gate)
        {
            // Match by file name too: a pack broken by an edit has no readable id, and that is
            // exactly when you need Reset.
            var existing = _catalog.Results.FirstOrDefault(r => r.IsBuiltIn && (r.Pack?.Id == id || Path.GetFileNameWithoutExtension(r.Source) == id))
                ?? throw new ArgumentException($"'{id}' is not a built-in pack.");
            _packLibrary.ResetToBuiltIn(Path.GetFileName(existing.Source));
        }
        Reload();
    }

    public void RestoreBuiltIns()
    {
        _packLibrary.RestoreRemoved();
        Reload();
    }

    /// <summary>Opens the library, data or themes folder (or a pack file's folder) in the OS file manager.</summary>
    public object OpenFolder(string which, string? packId)
    {
        string path = which switch
        {
            "library" => _paths.LibraryDir,
            "themes" => _paths.UserThemesDir,
            "data" => _paths.DataDir,
            "pack" => _catalog.Results.FirstOrDefault(r => r.Pack?.Id == packId)?.Source ?? throw new KeyNotFoundException("Pack not found."),
            _ => throw new ArgumentException($"Unknown folder '{which}'."),
        };
        if (OpenInShell == null) return new { opened = false, path };
        OpenInShell(path);
        return new { opened = true, path };
    }

    /// <summary>
    /// Polled by the frontend: if any pack or theme file changed on disk since the last load,
    /// reload and return the fresh bootstrap plus the names of packs that are now broken.
    /// </summary>
    public LibraryCheck CheckLibrary()
    {
        lock (_gate)
            if (Library.Fingerprint(_paths.LibraryDir, _paths.UserThemesDir) == _fingerprint)
                return new LibraryCheck(false, null, []);
        Reload();
        var broken = PackInfos().Where(p => !p.Usable).Select(p => $"{p.FileName}: {p.Errors.FirstOrDefault()}").ToList();
        return new LibraryCheck(true, Bootstrap(), broken);
    }

    public ExportResult Export(Concept concept, string format, string? notes, string? title)
    {
        var fmt = format.ToLowerInvariant() switch
        {
            "markdown" or "md" => ExportFormat.Markdown,
            "text" or "txt" => ExportFormat.Text,
            "json" => ExportFormat.Json,
            _ => throw new ArgumentException($"Unknown export format '{format}'."),
        };
        var mime = fmt switch { ExportFormat.Markdown => "text/markdown", ExportFormat.Text => "text/plain", _ => "application/json" };
        return new ExportResult(ConceptExporter.FileName(concept, fmt, title), mime, ConceptExporter.Export(concept, fmt, notes, title));
    }

    public string EncodeRecipe(Recipe recipe)
    {
        lock (_gate) return RecipeCode.Encode(Generator.Normalize(recipe), _library.Signature.Select(p => p.Id));
    }

    public StatsDto Stats()
    {
        var entries = Vault.List();
        lock (_gate)
        {
            List<KeyValuePair<string, int>> Top(string field) => entries
                .SelectMany(e => e.Concept.Picks.Where(p => p.Field == field).Select(p => p.Label))
                .GroupBy(l => l).Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(5).ToList();
            return new StatsDto
            {
                TotalGenerated = Settings.Current.TotalGenerated,
                VaultCount = entries.Count,
                FavoriteCount = entries.Count(e => e.Favorite),
                LibraryItems = _library.ItemCount,
                ItemsPerCategory = Categories.All.ToDictionary(c => c.Key, c => _library.Items(c.Key).Count),
                TopGenres = Top("genre"),
                TopSettings = Top("setting"),
                AverageChaos = entries.Count == 0 ? 0 : Math.Round(entries.Average(e => e.Concept.Recipe.Chaos), 2),
                CreepAccepted = entries.Sum(e => e.Concept.Creep.Count(c => c.Status == CreepStatus.Accepted)),
            };
        }
    }

    public const string NoorTheme = "noors-purple-heart";

    public AppSettings UpdateSettings(JsonElement patch)
    {
        Settings.Update(s =>
        {
            if (patch.TryGetProperty("theme", out var theme) && theme.ValueKind == JsonValueKind.String) s.Theme = theme.GetString()!;
            if (patch.TryGetProperty("outputMode", out var mode) && mode.GetString() is "oneliner" or "pitch" or "expanded") s.OutputMode = mode.GetString()!;
            if (patch.TryGetProperty("reduceMotion", out var rm) && rm.ValueKind is JsonValueKind.True or JsonValueKind.False) s.ReduceMotion = rm.GetBoolean();
            if (patch.TryGetProperty("noorMode", out var nm) && nm.ValueKind is JsonValueKind.True or JsonValueKind.False && nm.GetBoolean() != s.NoorMode)
            {
                s.NoorMode = nm.GetBoolean();
                if (s.NoorMode)
                {
                    s.ThemeBeforeNoor = s.Theme;
                    if (_themes.Any(t => t.Id == NoorTheme)) s.Theme = NoorTheme;
                }
                else
                {
                    s.Theme = s.ThemeBeforeNoor ?? "badsad-plum";
                    s.ThemeBeforeNoor = null;
                }
            }
        });
        return Settings.Current;
    }
}
