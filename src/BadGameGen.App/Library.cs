using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BadGameGen.App;

/// <summary>What the library knows about one built-in pack file.</summary>
public sealed class BuiltInRecord
{
    /// <summary>Hash of the version we last copied into the library (not the user's edits).</summary>
    public string InstalledHash { get; set; } = "";
    /// <summary>The user deleted this built-in on purpose; don't put it back automatically.</summary>
    public bool Removed { get; set; }
}

internal sealed class LibraryManifest
{
    public int Format { get; set; } = 1;
    public Dictionary<string, BuiltInRecord> BuiltIns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Library state of one built-in pack file, for the Packs page.</summary>
public sealed record BuiltInState(string FileName, bool Edited, bool UpdateAvailable, bool Removed);

/// <summary>
/// The pack library: one user-visible, editable folder holding every pack (built-in copies and
/// imported packs alike). The packs embedded in / shipped with the app are only the factory
/// defaults that seed it.
///
/// Rules for built-in files on every start-up (see <see cref="Sync"/>):
///  - missing and never installed → copy it in
///  - unedited (hash matches what we installed) and the app ships a newer one → update it
///  - edited by the user → never touched; flagged "update available" if the app has a newer one
///  - deleted by the user → stays deleted until "Restore built-in packs"
/// Hashes ignore line endings, so opening and re-saving a file in Notepad doesn't count as an edit.
/// </summary>
public sealed class Library
{
    private const string ManifestName = ".builtin-packs.json";
    private readonly string _shippedDir;
    private readonly object _gate = new();
    private LibraryManifest _manifest = new();

    public string Directory { get; }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public Library(string shippedDir, string libraryDir)
    {
        _shippedDir = shippedDir;
        Directory = libraryDir;
        System.IO.Directory.CreateDirectory(libraryDir);
        LoadManifest();
    }

    private string ManifestPath => Path.Combine(Directory, ManifestName);

    private void LoadManifest()
    {
        try
        {
            if (File.Exists(ManifestPath))
                _manifest = JsonSerializer.Deserialize<LibraryManifest>(File.ReadAllText(ManifestPath), Json) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _manifest = new(); // worst case we treat existing files as "edited" and leave them alone
        }
        _manifest.BuiltIns = new(_manifest.BuiltIns ?? new(), StringComparer.OrdinalIgnoreCase);
    }

    private void SaveManifest()
    {
        var tmp = ManifestPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_manifest, Json));
        File.Move(tmp, ManifestPath, overwrite: true);
    }

    public static string Hash(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private IEnumerable<string> ShippedFiles() =>
        System.IO.Directory.Exists(_shippedDir)
            ? System.IO.Directory.EnumerateFiles(_shippedDir, "*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)
            : [];

    /// <summary>Brings the library in line with the shipped packs. Safe to call on every start.</summary>
    public void Sync()
    {
        lock (_gate)
        {
            foreach (var file in ShippedFiles())
            {
                var shipped = Path.Combine(_shippedDir, file);
                var target = Path.Combine(Directory, file);
                var shippedHash = Hash(shipped);
                _manifest.BuiltIns.TryGetValue(file, out var rec);

                if (!File.Exists(target))
                {
                    if (rec == null)
                    {
                        File.Copy(shipped, target);
                        _manifest.BuiltIns[file] = new BuiltInRecord { InstalledHash = shippedHash };
                    }
                    else
                    {
                        rec.Removed = true; // it was installed before, so the user deleted it
                    }
                    continue;
                }

                if (rec == null)
                {
                    // A file we didn't install (e.g. from an older version). Never overwrite it blindly.
                    _manifest.BuiltIns[file] = new BuiltInRecord { InstalledHash = shippedHash };
                    continue;
                }
                rec.Removed = false;
                if (Hash(target) == rec.InstalledHash && shippedHash != rec.InstalledHash)
                {
                    File.Copy(shipped, target, overwrite: true); // unedited: take the new version
                    rec.InstalledHash = shippedHash;
                }
            }
            SaveManifest();
        }
    }

    public bool IsBuiltInFile(string fileName) =>
        _manifest.BuiltIns.TryGetValue(fileName, out var rec) && !rec.Removed && File.Exists(Path.Combine(_shippedDir, fileName));

    public BuiltInState? State(string fileName)
    {
        lock (_gate)
        {
            if (!IsBuiltInFile(fileName)) return null;
            var rec = _manifest.BuiltIns[fileName];
            var target = Path.Combine(Directory, fileName);
            bool edited = File.Exists(target) && Hash(target) != rec.InstalledHash;
            bool update = edited && Hash(Path.Combine(_shippedDir, fileName)) != rec.InstalledHash;
            return new BuiltInState(fileName, edited, update, rec.Removed);
        }
    }

    /// <summary>Built-in files the user deleted, which "Restore built-in packs" can bring back.</summary>
    public List<string> RemovedBuiltIns()
    {
        lock (_gate)
            return _manifest.BuiltIns.Where(kv => kv.Value.Removed && File.Exists(Path.Combine(_shippedDir, kv.Key)))
                .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>Replaces the library copy with the shipped version (discarding edits).</summary>
    public void ResetToBuiltIn(string fileName)
    {
        lock (_gate)
        {
            var shipped = Path.Combine(_shippedDir, Path.GetFileName(fileName));
            if (!File.Exists(shipped)) throw new ArgumentException($"'{fileName}' is not a built-in pack.");
            File.Copy(shipped, Path.Combine(Directory, Path.GetFileName(fileName)), overwrite: true);
            _manifest.BuiltIns[Path.GetFileName(fileName)] = new BuiltInRecord { InstalledHash = Hash(shipped) };
            SaveManifest();
        }
    }

    public void RestoreRemoved()
    {
        foreach (var file in RemovedBuiltIns()) ResetToBuiltIn(file);
    }

    /// <summary>Deletes a pack file from the library. Built-ins are remembered as removed.</summary>
    public void Remove(string fileName)
    {
        lock (_gate)
        {
            var path = Path.Combine(Directory, Path.GetFileName(fileName));
            if (File.Exists(path)) File.Delete(path);
            if (_manifest.BuiltIns.TryGetValue(Path.GetFileName(fileName), out var rec)) rec.Removed = true;
            SaveManifest();
        }
    }

    /// <summary>Cheap fingerprint of the folder (names, sizes, write times) for change detection.</summary>
    public static string Fingerprint(params string[] dirs)
    {
        var sb = new StringBuilder();
        foreach (var dir in dirs.Where(System.IO.Directory.Exists))
            foreach (var f in new DirectoryInfo(dir).EnumerateFiles("*.json").Where(f => !f.Name.StartsWith('.')).OrderBy(f => f.Name, StringComparer.Ordinal))
                sb.Append(f.Name).Append('|').Append(f.Length).Append('|').Append(f.LastWriteTimeUtc.Ticks).Append(';');
        return sb.ToString();
    }
}
