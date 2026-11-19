using System.Text.Json;
using BadGameGen.Core.Generation;

namespace BadGameGen.App;

/// <summary>User preferences. Unknown/missing values fall back to defaults.</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "badsad-plum";
    /// <summary>Packs the user switched off. Stored as "disabled" so newly added packs start enabled.</summary>
    public List<string> DisabledPacks { get; set; } = new();
    /// <summary>"oneliner", "pitch" or "expanded".</summary>
    public string OutputMode { get; set; } = "pitch";
    /// <summary>The last recipe used, restored on start so the app reopens where you left it.</summary>
    public Recipe? LastRecipe { get; set; }
    public int TotalGenerated { get; set; }
    public bool ReduceMotion { get; set; }
}

public sealed class SettingsStore
{
    private readonly string _path;
    private readonly object _gate = new();
    public AppSettings Current { get; private set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public SettingsStore(string path)
    {
        _path = path;
        try
        {
            if (File.Exists(path))
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            Current = new AppSettings(); // settings are disposable; never block startup on them
        }
        Current.DisabledPacks ??= new();
    }

    public void Update(Action<AppSettings> change)
    {
        lock (_gate)
        {
            change(Current);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, Options));
            File.Move(tmp, _path, overwrite: true);
        }
    }
}
