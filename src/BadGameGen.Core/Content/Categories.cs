namespace BadGameGen.Core.Content;

/// <summary>Describes one content library category and the text "forms" every item must provide.</summary>
public sealed record CategoryInfo(
    string Key,
    string DisplayName,
    string Icon,
    IReadOnlyList<string> RequiredForms,
    IReadOnlyList<string> OptionalForms,
    IReadOnlyList<string> FormsAllowedEmpty,
    string Description,
    bool IsTemplateForms = false);

/// <summary>
/// The fixed set of categories the generator understands. Packs may only add items to these.
/// Forms are the grammatical building blocks templates use, e.g. setting.in = "in a flooded Baltic town".
/// See docs/CONTENT_GUIDE.md for how each form is used in a sentence.
/// </summary>
public static class Categories
{
    public const string Genre = "genre";
    public const string Subgenre = "subgenre";
    public const string Setting = "setting";
    public const string Era = "era";
    public const string Protagonist = "protagonist";
    public const string Perspective = "perspective";
    public const string Objective = "objective";
    public const string Loop = "loop";
    public const string Mechanic = "mechanic";
    public const string Progression = "progression";
    public const string Threat = "threat";
    public const string Resource = "resource";
    public const string Mood = "mood";
    public const string Visual = "visual";
    public const string Twist = "twist";
    public const string Multiplayer = "multiplayer";
    public const string TitleFirst = "titleFirst";
    public const string TitleLast = "titleLast";
    public const string Creep = "creep";

    private static readonly string[] None = [];

    public static readonly IReadOnlyList<CategoryInfo> All =
    [
        new(Genre, "Genre", "gamepad", ["adj", "noun"], None, None, "Broad genre. adj: 'survival', noun: 'survival game'."),
        new(Subgenre, "Subgenre", "shapes", ["noun"], None, None, "Game type noun used after the genre adjective: 'management game', 'deckbuilder'."),
        new(Setting, "Setting", "map-pin", ["noun", "in"], None, None, "noun: 'flooded Baltic town'; in: 'in a flooded Baltic town'."),
        new(Era, "Era", "hourglass", ["in"], None, ["in"], "Time period phrase: 'in the late 1980s'. May be empty for 'present day'."),
        new(Protagonist, "Protagonist", "user", ["noun"], None, None, "Singular role without article: 'stranded radio operator'."),
        new(Perspective, "Perspective", "camera", ["noun"], None, None, "Camera/viewpoint noun phrase: 'top-down view'."),
        new(Objective, "Objective", "flag", ["verb"], None, None, "Base-form verb phrase: 'keep the survivors connected'."),
        new(Loop, "Gameplay Loop", "repeat", ["verb"], None, None, "Base-form loop phrase following 'you': 'scavenge by day and fortify by night'."),
        new(Mechanic, "Mechanic", "cog", ["noun", "verb"], None, None, "noun: 'radio triangulation'; verb: 'triangulate radio signals'."),
        new(Progression, "Progression", "trending-up", ["verb"], None, None, "Base-form phrase following 'you': 'upgrade your antenna network'."),
        new(Threat, "Threat", "skull", ["noun", "clause"], None, None, "noun: 'rising water'; clause (full clause, present tense): 'the water creeps higher every day'."),
        new(Resource, "Resource", "gem", ["noun"], None, None, "Plural or uncountable noun: 'fuel', 'spare batteries'."),
        new(Mood, "Tone", "smile", ["adj"], None, None, "Adjective: 'moody', 'darkly comic'."),
        new(Visual, "Visual Style", "image", ["noun"], None, None, "Noun phrase: 'retro 3D with chunky textures'."),
        new(Twist, "Twist", "sparkles", ["clause"], None, None, "Full clause: 'the coastline changes every day'."),
        new(Multiplayer, "Players", "users", ["phrase"], None, None, "Sentence fragment: 'play solo or in two-player co-op'."),
        new(TitleFirst, "Title (first word)", "type", None, None, None, "Title opener word(s). The label is used as-is."),
        new(TitleLast, "Title (last word)", "type", None, None, None, "Title closing word(s). The label is used as-is."),
        new(Creep, "Feature Creep", "flame", ["pitch"], ["connects"], None,
            "Feature Creep proposal. pitch/connects are templates that may reference concept fields.", IsTemplateForms: true),
    ];

    private static readonly Dictionary<string, CategoryInfo> ByKey = All.ToDictionary(c => c.Key);

    public static bool TryGet(string key, out CategoryInfo info) => ByKey.TryGetValue(key, out info!);

    public static CategoryInfo Get(string key) => ByKey[key];

    /// <summary>Valid values for creep item "kind".</summary>
    public static readonly IReadOnlyList<string> CreepKinds = ["system", "loop", "content", "whyNot"];
}
