using System.Text.Json.Serialization;

namespace BadGameGen.Core.Content;

/// <summary>One entry in a content library (a genre, a twist, a creep proposal...).</summary>
public sealed class ContentItem
{
    /// <summary>Pack-local id, e.g. "survival". Full id is "packId:category.id".</summary>
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public Dictionary<string, string> Forms { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public double Weight { get; set; } = 1.0;
    /// <summary>Tags this item likes to appear alongside (boosts weight when present).</summary>
    public List<string> Affinity { get; set; } = new();
    /// <summary>Tags this item refuses to appear alongside (unless chaos is high).</summary>
    public List<string> Excludes { get; set; } = new();
    /// <summary>0 = grounded, 1 = utterly unhinged. Chaos slider favours matching values.</summary>
    public double Weirdness { get; set; } = 0.25;
    /// <summary>0 = deadly serious, 1 = clown shoes. Serious/silly slider favours matching values.</summary>
    public double Silliness { get; set; } = 0.3;
    /// <summary>Optional [min, max] scope levels (0 = tiny jam ... 4 = absurd) this item suits.</summary>
    public int[]? Scope { get; set; }

    // Feature creep only
    public string? Kind { get; set; }
    public int Complexity { get; set; }

    // Filled in by the loader
    [JsonIgnore] public string PackId { get; set; } = "";
    [JsonIgnore] public string Category { get; set; } = "";
    [JsonIgnore] public string FullId => $"{PackId}:{Category}.{Id}";

    public string Form(string name) =>
        name == "label" ? Label : (Forms.TryGetValue(name, out var v) ? v : Label);
}

/// <summary>A sentence template. Text uses {field.form} references; see TemplateEngine.</summary>
public sealed class TemplateDef
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public double Weight { get; set; } = 1.0;
    /// <summary>Optional scope levels this template is allowed for.</summary>
    public int[]? Scopes { get; set; }
    /// <summary>Optional: template only eligible when the concept has at least one of these tags.</summary>
    public List<string>? TagsAny { get; set; }

    [JsonIgnore] public string PackId { get; set; } = "";
    [JsonIgnore] public string Section { get; set; } = "";
    [JsonIgnore] public string FullId => $"{PackId}:{Section}.{Id}";
}

/// <summary>A content pack as stored on disk (JSON).</summary>
public sealed class ContentPack
{
    public int Format { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, List<ContentItem>> Items { get; set; } = new();
    public Dictionary<string, List<TemplateDef>> Templates { get; set; } = new();
}

/// <summary>Template sections the generator reads. Packs may only add to these.</summary>
public static class TemplateSections
{
    public const string Title = "title";
    public const string Hook = "hook";
    public const string PitchOpen = "pitchOpen";
    public const string PitchLoop = "pitchLoop";
    public const string PitchSystems = "pitchSystems";
    public const string PitchThreat = "pitchThreat";
    public const string PitchTwist = "pitchTwist";
    public const string PitchStyle = "pitchStyle";
    public const string BadIdea = "badIdea";
    public const string MightWork = "mightWork";
    public const string ScopeNote = "scopeNote";

    public static readonly IReadOnlyList<string> All =
        [Title, Hook, PitchOpen, PitchLoop, PitchSystems, PitchThreat, PitchTwist, PitchStyle, BadIdea, MightWork, ScopeNote];

    /// <summary>Order the short pitch paragraph is assembled in.</summary>
    public static readonly IReadOnlyList<string> PitchOrder =
        [PitchOpen, PitchLoop, PitchSystems, PitchThreat, PitchTwist, PitchStyle];
}
