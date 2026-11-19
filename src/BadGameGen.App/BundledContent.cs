using System.Reflection;

namespace BadGameGen.App;

/// <summary>
/// Lets a single-file .exe carry its own content packs, themes and frontend.
///
/// Release builds embed those files as resources named "bundle/&lt;relative path&gt;". On start-up
/// we prefer loose files next to the executable (development builds, or users who want to edit
/// packs in place); otherwise the embedded files are unpacked once per build into
/// &lt;dataDir&gt;/app/&lt;build id&gt;/ and older unpacked builds are removed.
/// </summary>
public static class BundledContent
{
    public const string Prefix = "bundle/";

    /// <summary>Returns the folder that contains "content" (and "wwwroot" for the desktop app).</summary>
    public static string ResolveAppRoot(Assembly assembly, string dataDir)
    {
        var baseDir = AppContext.BaseDirectory;
        if (Directory.Exists(Path.Combine(baseDir, "content", "packs"))) return baseDir;

        var resources = assembly.GetManifestResourceNames().Where(n => n.StartsWith(Prefix, StringComparison.Ordinal)).ToList();
        if (resources.Count == 0) return baseDir;

        var appsDir = Path.Combine(dataDir, "app");
        var target = Path.Combine(appsDir, assembly.ManifestModule.ModuleVersionId.ToString("N"));
        var marker = Path.Combine(target, ".complete");
        if (File.Exists(marker)) return target;

        if (Directory.Exists(target)) Directory.Delete(target, recursive: true); // half-finished earlier attempt
        var root = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
        foreach (var name in resources)
        {
            var relative = name[Prefix.Length..].Replace('\\', '/');
            var path = Path.GetFullPath(Path.Combine(target, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // never write outside target
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var input = assembly.GetManifestResourceStream(name)!;
            using var output = File.Create(path);
            input.CopyTo(output);
        }
        File.WriteAllText(marker, DateTimeOffset.UtcNow.ToString("O"));

        foreach (var old in Directory.GetDirectories(appsDir).Where(d => !string.Equals(Path.GetFullPath(d), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)))
        {
            try { Directory.Delete(old, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        return target;
    }
}
