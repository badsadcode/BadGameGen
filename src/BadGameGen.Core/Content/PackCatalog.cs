namespace BadGameGen.Core.Content;

/// <summary>Every pack found on disk (usable or not), plus helpers to build a library from them.</summary>
public sealed class PackCatalog
{
    public IReadOnlyList<PackLoadResult> Results { get; }

    public PackCatalog(IReadOnlyList<PackLoadResult> results) => Results = results;

    public IEnumerable<ContentPack> UsablePacks => Results.Where(r => r.IsUsable).Select(r => r.Pack!);

    /// <summary>Loads every *.json pack in the given directories. Missing directories are skipped.</summary>
    public static PackCatalog Load(IEnumerable<(string Directory, bool BuiltIn)> sources)
    {
        var results = new List<PackLoadResult>();
        var seen = new Dictionary<string, string>();
        foreach (var (dir, builtIn) in sources)
        {
            if (!Directory.Exists(dir)) continue;
            // Hidden files (".builtin-packs.json" etc.) are bookkeeping, not packs.
            foreach (var file in Directory.EnumerateFiles(dir, "*.json").Where(f => !Path.GetFileName(f).StartsWith('.')).Order(StringComparer.Ordinal))
            {
                var result = PackLoader.LoadFile(file, builtIn);
                if (result.Pack is { } pack && !string.IsNullOrEmpty(pack.Id))
                {
                    if (seen.TryGetValue(pack.Id, out var other))
                        result.Errors.Add($"Duplicate pack id '{pack.Id}' (already loaded from {Path.GetFileName(other)}).");
                    else
                        seen[pack.Id] = file;
                }
                results.Add(result);
            }
        }
        return new PackCatalog(results);
    }

    /// <summary>Builds a library from the usable packs whose ids are enabled (null = all usable).</summary>
    public ContentLibrary BuildLibrary(IEnumerable<string>? enabledIds = null)
    {
        var enabled = enabledIds == null ? null : new HashSet<string>(enabledIds);
        return new ContentLibrary(UsablePacks.Where(p => enabled == null || enabled.Contains(p.Id)));
    }
}
