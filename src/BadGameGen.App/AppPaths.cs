namespace BadGameGen.App;

/// <summary>Where BadGameGen reads built-in content and stores user data.</summary>
public sealed record AppPaths(string ContentDir, string DataDir)
{
    public string BuiltInPacksDir => Path.Combine(ContentDir, "packs");
    public string BuiltInThemesDir => Path.Combine(ContentDir, "themes");
    public string UserPacksDir => Path.Combine(DataDir, "packs");
    public string UserThemesDir => Path.Combine(DataDir, "themes");
    public string VaultPath => Path.Combine(DataDir, "vault.json");
    public string SettingsPath => Path.Combine(DataDir, "settings.json");

    /// <summary>Per-user data folder: %LOCALAPPDATA%\BadGameGen on Windows, ~/.local/share/BadGameGen elsewhere.</summary>
    public static string DefaultDataDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BadGameGen");

    /// <summary>Finds content/ next to the binary, else walks up from the working directory (dev).</summary>
    public static string FindContentDir()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "content");
        if (Directory.Exists(local)) return local;
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (Directory.Exists(Path.Combine(candidate, "packs"))) return candidate;
            dir = dir.Parent;
        }
        return local;
    }
}
