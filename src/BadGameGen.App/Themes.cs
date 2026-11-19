using System.Text.Json;
using System.Text.RegularExpressions;

namespace BadGameGen.App;

/// <summary>A colour scheme. Colours become CSS custom properties (--c-bg, --c-accent, ...).</summary>
public sealed class ThemeDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Dark { get; set; } = true;
    public Dictionary<string, string> Colors { get; set; } = new();
    public bool IsBuiltIn { get; set; }
}

public static partial class ThemeCatalog
{
    /// <summary>Every theme must define these. The frontend relies on them.</summary>
    public static readonly string[] RequiredColors =
    [
        "bg", "bgGlow", "surface", "surfaceRaised", "border", "text", "textMuted",
        "accent", "accentStrong", "accent2", "glow", "success", "warning", "danger",
    ];

    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|rgba?\([0-9.,\s%]+\))$")]
    private static partial Regex ColorRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex IdRegex();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static (List<ThemeDef> Themes, List<string> Errors) Load(IEnumerable<(string Dir, bool BuiltIn)> sources)
    {
        var themes = new List<ThemeDef>();
        var errors = new List<string>();
        foreach (var (dir, builtIn) in sources)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.json").Order(StringComparer.Ordinal))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<ThemeDef>(File.ReadAllText(file), Options);
                    var problems = Validate(theme);
                    if (problems.Count > 0) { errors.Add($"{Path.GetFileName(file)}: {string.Join("; ", problems)}"); continue; }
                    if (themes.Any(t => t.Id == theme!.Id)) { errors.Add($"{Path.GetFileName(file)}: duplicate theme id '{theme!.Id}'."); continue; }
                    theme!.IsBuiltIn = builtIn;
                    themes.Add(theme);
                }
                catch (Exception ex) when (ex is JsonException or IOException)
                {
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
        return (themes, errors);
    }

    public static List<string> Validate(ThemeDef? theme)
    {
        var problems = new List<string>();
        if (theme == null) return ["empty file"];
        if (!IdRegex().IsMatch(theme.Id ?? "")) problems.Add("id must be lowercase letters, digits and dashes");
        if (string.IsNullOrWhiteSpace(theme.Name)) problems.Add("name is missing");
        theme.Colors ??= new();
        foreach (var key in RequiredColors)
        {
            if (!theme.Colors.TryGetValue(key, out var value)) problems.Add($"missing color '{key}'");
            else if (!ColorRegex().IsMatch(value.Trim())) problems.Add($"color '{key}' must be #hex or rgb()/rgba()");
        }
        foreach (var (key, value) in theme.Colors)
            if (!RequiredColors.Contains(key) && !ColorRegex().IsMatch((value ?? "").Trim()))
                problems.Add($"color '{key}' must be #hex or rgb()/rgba()");
        return problems;
    }
}
