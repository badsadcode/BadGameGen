using System.Text.Json;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Vault;

/// <summary>A saved idea. Stores the full rendered concept so it survives library changes.</summary>
public sealed class VaultEntry
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public Concept Concept { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; } = "";
    public bool Favorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class VaultFile
{
    public int Format { get; set; } = 1;
    public List<VaultEntry> Entries { get; set; } = new();
}

/// <summary>
/// Local JSON persistence for the Idea Vault. One file, written atomically (temp file + replace)
/// so a crash mid-save can never leave a half-written vault. A corrupt vault is moved aside,
/// never deleted.
/// </summary>
public sealed class VaultStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly Func<DateTimeOffset> _clock;
    private List<VaultEntry> _entries = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    /// <summary>Set when loading found a damaged file (and where it was backed up).</summary>
    public string? RecoveryNotice { get; private set; }

    public VaultStore(string path, Func<DateTimeOffset>? clock = null)
    {
        _path = path;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var file = JsonSerializer.Deserialize<VaultFile>(File.ReadAllText(_path), JsonOptions);
            _entries = file?.Entries?.Where(e => e != null && !string.IsNullOrEmpty(e.Id)).ToList() ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            var backup = _path + $".corrupt-{_clock():yyyyMMdd-HHmmss}";
            try { File.Move(_path, backup); } catch (IOException) { }
            RecoveryNotice = $"The vault file was damaged and has been set aside as {Path.GetFileName(backup)}. Starting with an empty vault.";
            _entries = new();
        }
    }

    private void Persist()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new VaultFile { Entries = _entries }, JsonOptions));
        File.Move(tmp, _path, overwrite: true);
    }

    private static T Copy<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;

    public IReadOnlyList<VaultEntry> List()
    {
        lock (_gate) return _entries.OrderByDescending(e => e.UpdatedAt).Select(Copy).ToList();
    }

    public VaultEntry? Get(string id)
    {
        lock (_gate) return _entries.FirstOrDefault(e => e.Id == id) is { } e ? Copy(e) : null;
    }

    public VaultEntry Save(Concept concept, string? notes = null, IEnumerable<string>? tags = null)
    {
        lock (_gate)
        {
            var now = _clock();
            var entry = new VaultEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = concept.Title,
                Concept = Copy(concept),
                Notes = notes ?? "",
                Tags = tags?.Distinct().ToList() ?? concept.Chips.Select(c => c.Label).ToList(),
                CreatedAt = now,
                UpdatedAt = now,
            };
            _entries.Add(entry);
            Persist();
            return Copy(entry);
        }
    }

    public sealed record Patch(string? Title = null, string? Notes = null, List<string>? Tags = null, bool? Favorite = null, Concept? Concept = null);

    public VaultEntry Update(string id, Patch patch)
    {
        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id) ?? throw new KeyNotFoundException($"No vault entry '{id}'.");
            if (patch.Title != null)
            {
                entry.Title = patch.Title.Trim().Length > 0 ? patch.Title.Trim() : entry.Title;
            }
            if (patch.Notes != null) entry.Notes = patch.Notes;
            if (patch.Tags != null) entry.Tags = patch.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
            if (patch.Favorite != null) entry.Favorite = patch.Favorite.Value;
            if (patch.Concept != null) entry.Concept = Copy(patch.Concept);
            entry.UpdatedAt = _clock();
            Persist();
            return Copy(entry);
        }
    }

    public VaultEntry Duplicate(string id)
    {
        lock (_gate)
        {
            var source = _entries.FirstOrDefault(e => e.Id == id) ?? throw new KeyNotFoundException($"No vault entry '{id}'.");
            var now = _clock();
            var copy = Copy(source);
            copy.Id = Guid.NewGuid().ToString("N");
            copy.Title = source.Title + " (copy)";
            copy.Favorite = false;
            copy.CreatedAt = now;
            copy.UpdatedAt = now;
            _entries.Add(copy);
            Persist();
            return Copy(copy);
        }
    }

    public bool Delete(string id)
    {
        lock (_gate)
        {
            int removed = _entries.RemoveAll(e => e.Id == id);
            if (removed > 0) Persist();
            return removed > 0;
        }
    }
}
