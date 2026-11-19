using System.Text.Json;
using System.Text.RegularExpressions;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Content;

/// <summary>Outcome of loading one pack file. A pack with errors is never used for generation.</summary>
public sealed class PackLoadResult
{
    public ContentPack? Pack { get; init; }
    public string Source { get; init; } = "";
    public bool IsBuiltIn { get; init; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsUsable => Pack != null && Errors.Count == 0;
}

public static partial class PackLoader
{
    public const int CurrentFormat = 1;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex IdRegex();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static PackLoadResult LoadFile(string path, bool isBuiltIn)
    {
        try
        {
            return LoadJson(File.ReadAllText(path), path, isBuiltIn);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var r = new PackLoadResult { Source = path, IsBuiltIn = isBuiltIn };
            r.Errors.Add($"Could not read file: {ex.Message}");
            return r;
        }
    }

    public static PackLoadResult LoadJson(string json, string source, bool isBuiltIn = false)
    {
        ContentPack? pack;
        try
        {
            pack = JsonSerializer.Deserialize<ContentPack>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            var bad = new PackLoadResult { Source = source, IsBuiltIn = isBuiltIn };
            bad.Errors.Add($"Invalid JSON (line {ex.LineNumber + 1}): {ex.Message}");
            return bad;
        }
        if (pack == null)
        {
            var empty = new PackLoadResult { Source = source, IsBuiltIn = isBuiltIn };
            empty.Errors.Add("File is empty or not a JSON object.");
            return empty;
        }
        var result = new PackLoadResult { Pack = pack, Source = source, IsBuiltIn = isBuiltIn };
        Validate(pack, result.Errors, result.Warnings);
        return result;
    }

    /// <summary>Validates a pack and stamps pack/category info onto its items and templates.</summary>
    public static void Validate(ContentPack pack, List<string> errors, List<string> warnings)
    {
        pack.Items ??= new();
        pack.Templates ??= new();

        if (pack.Format != CurrentFormat)
            errors.Add($"Unsupported format {pack.Format} (expected {CurrentFormat}).");
        if (!IdRegex().IsMatch(pack.Id ?? ""))
            errors.Add($"Pack id '{pack.Id}' must be lowercase letters, digits and dashes.");
        if (string.IsNullOrWhiteSpace(pack.Name)) errors.Add("Pack name is missing.");
        if (string.IsNullOrWhiteSpace(pack.Version)) errors.Add("Pack version is missing.");

        foreach (var (category, items) in pack.Items)
        {
            if (!Categories.TryGet(category, out var info))
            {
                errors.Add($"Unknown category '{category}'. Valid: {string.Join(", ", Categories.All.Select(c => c.Key))}.");
                continue;
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < (items?.Count ?? 0); i++)
            {
                var item = items![i];
                var where = $"{category}[{i}] '{item?.Id}'";
                if (item == null) { errors.Add($"{category}[{i}] is null."); continue; }
                item.PackId = pack.Id ?? "";
                item.Category = category;
                item.Forms ??= new();
                item.Tags ??= new();
                item.Affinity ??= new();
                item.Excludes ??= new();

                if (!IdRegex().IsMatch(item.Id ?? "")) errors.Add($"{where}: id must be lowercase letters, digits and dashes.");
                else if (!seen.Add(item.Id!)) errors.Add($"{where}: duplicate id in category.");
                if (string.IsNullOrWhiteSpace(item.Label)) errors.Add($"{where}: label is missing.");
                if (!(item.Weight > 0) || !double.IsFinite(item.Weight)) errors.Add($"{where}: weight must be a positive number.");
                if (item.Weirdness is < 0 or > 1) errors.Add($"{where}: weirdness must be between 0 and 1.");
                if (item.Silliness is < 0 or > 1) errors.Add($"{where}: silliness must be between 0 and 1.");
                if (item.Scope != null && (item.Scope.Length != 2 || item.Scope[0] < 0 || item.Scope[1] > 4 || item.Scope[0] > item.Scope[1]))
                    errors.Add($"{where}: scope must be [min, max] within 0..4.");

                foreach (var form in info.RequiredForms)
                {
                    if (!item.Forms.TryGetValue(form, out var value) || value == null)
                        errors.Add($"{where}: missing required form '{form}'.");
                    else if (value.Trim().Length == 0 && !info.FormsAllowedEmpty.Contains(form))
                        errors.Add($"{where}: form '{form}' must not be empty.");
                }
                foreach (var (formName, value) in item.Forms)
                {
                    if (!info.RequiredForms.Contains(formName) && !info.OptionalForms.Contains(formName))
                        warnings.Add($"{where}: form '{formName}' is not used by category '{category}'.");
                    if (value == null) continue;
                    if (info.IsTemplateForms)
                    {
                        foreach (var e in TemplateEngine.Validate(value)) errors.Add($"{where}.{formName}: {e}");
                    }
                    else if (value.Contains('{') || value.Contains('}'))
                    {
                        errors.Add($"{where}.{formName}: braces are only allowed in template forms.");
                    }
                }

                if (category == Categories.Creep)
                {
                    if (item.Kind == null || !Categories.CreepKinds.Contains(item.Kind))
                        errors.Add($"{where}: kind must be one of {string.Join(", ", Categories.CreepKinds)}.");
                    if (item.Complexity is < 1 or > 10) errors.Add($"{where}: complexity must be 1..10.");
                }
            }
        }

        foreach (var (section, templates) in pack.Templates)
        {
            if (!TemplateSections.All.Contains(section))
            {
                errors.Add($"Unknown template section '{section}'. Valid: {string.Join(", ", TemplateSections.All)}.");
                continue;
            }
            var seen = new HashSet<string>();
            IReadOnlySet<string>? allowed = section == TemplateSections.Title
                ? new HashSet<string>(TemplateEngine.FieldCategories.Keys)
                : new HashSet<string>(TemplateEngine.FieldCategories.Keys.Where(k => k is not ("first" or "last")).Append("title"));
            for (int i = 0; i < (templates?.Count ?? 0); i++)
            {
                var t = templates![i];
                var where = $"templates.{section}[{i}] '{t?.Id}'";
                if (t == null) { errors.Add($"templates.{section}[{i}] is null."); continue; }
                t.PackId = pack.Id ?? "";
                t.Section = section;
                if (!IdRegex().IsMatch(t.Id ?? "")) errors.Add($"{where}: id must be lowercase letters, digits and dashes.");
                else if (!seen.Add(t.Id!)) errors.Add($"{where}: duplicate id in section.");
                if (string.IsNullOrWhiteSpace(t.Text)) errors.Add($"{where}: text is missing.");
                else foreach (var e in TemplateEngine.Validate(t.Text, allowed)) errors.Add($"{where}: {e}");
                if (!(t.Weight > 0) || !double.IsFinite(t.Weight)) errors.Add($"{where}: weight must be a positive number.");
                if (t.Scopes != null && t.Scopes.Any(s => s is < 0 or > 4)) errors.Add($"{where}: scopes must be within 0..4.");
            }
        }
    }
}
