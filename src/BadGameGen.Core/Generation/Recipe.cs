namespace BadGameGen.Core.Generation;

/// <summary>
/// Everything needed to reproduce a concept exactly (together with the same content packs).
///
/// Reproducibility model:
///  - <see cref="Seed"/> drives every random choice.
///  - Each slot (field) draws from its own random stream: (seed, slot key, reroll nonce).
///    Rerolling one field just bumps its nonce in <see cref="Rerolls"/>; nothing else moves.
///  - <see cref="Pins"/> are locked/hand-picked items and bypass randomness entirely.
///  - "Generate Trouble" = new seed (pins survive, nonces reset).
/// </summary>
public sealed class Recipe
{
    /// <summary>Any text. Numbers and words both work; case and surrounding spaces are ignored.</summary>
    public string Seed { get; set; } = "";
    /// <summary>0 = grounded, 1 = audacious.</summary>
    public double Chaos { get; set; } = 0.35;
    /// <summary>0 tiny jam, 1 small indie, 2 medium, 3 ambitious, 4 absurd.</summary>
    public int Scope { get; set; } = 1;
    /// <summary>0 = serious, 1 = silly.</summary>
    public double Silliness { get; set; } = 0.5;
    /// <summary>Slot key → full item id ("core:genre.survival"). Locked or hand-picked fields.</summary>
    public Dictionary<string, string> Pins { get; set; } = new();
    /// <summary>
    /// Slot key → full item id held over from the previous concept without being "locked".
    /// A single-field reroll keeps every other field this way, so only the rerolled field changes
    /// (otherwise compatibility rules could nudge neighbours). "Generate Trouble" clears it.
    /// </summary>
    public Dictionary<string, string> Keep { get; set; } = new();
    /// <summary>Slot key → reroll count. Special keys: "title" and "wording".</summary>
    public Dictionary<string, int> Rerolls { get; set; } = new();
    /// <summary>Full item ids that must never be picked.</summary>
    public List<string> Exclusions { get; set; } = new();
    /// <summary>When set, used verbatim as the title (locked or renamed title).</summary>
    public string? TitleOverride { get; set; }

    public bool FeatureCreep { get; set; }
    /// <summary>Which kinds of creep may be proposed: system, loop, content, whyNot.</summary>
    public List<string> CreepKinds { get; set; } = ["system", "loop", "content", "whyNot"];
    /// <summary>Feature Creep slots, in order. Rejected slots stay so later slots keep their streams.</summary>
    public List<CreepSlot> Creep { get; set; } = new();

    public int RerollCount(string key) => Rerolls.TryGetValue(key, out var n) ? n : 0;

    public Recipe Clone() => new()
    {
        Seed = Seed,
        Chaos = Chaos,
        Scope = Scope,
        Silliness = Silliness,
        Pins = new(Pins),
        Keep = new(Keep),
        Rerolls = new(Rerolls),
        Exclusions = new(Exclusions),
        TitleOverride = TitleOverride,
        FeatureCreep = FeatureCreep,
        CreepKinds = new(CreepKinds),
        Creep = Creep.Select(c => new CreepSlot { Nonce = c.Nonce, Status = c.Status }).ToList(),
    };
}

public sealed class CreepSlot
{
    public int Nonce { get; set; }
    /// <summary>"proposed", "accepted" or "rejected".</summary>
    public string Status { get; set; } = CreepStatus.Proposed;
}

public static class CreepStatus
{
    public const string Proposed = "proposed";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public static readonly IReadOnlyList<string> All = [Proposed, Accepted, Rejected];
}

/// <summary>One field position in a concept, e.g. "genre" or "secondary#1".</summary>
public sealed record SlotDef(string Key, string Field, string Category);

public static class Scopes
{
    public static readonly IReadOnlyList<string> Names = ["Tiny jam", "Small indie", "Medium", "Ambitious", "Absurd"];
    public static readonly IReadOnlyList<string> Letters = ["T", "S", "M", "A", "X"];

    public static int SecondaryCount(int scope) => Math.Clamp(scope, 0, 4);
    public static bool HasProgression(int scope) => scope >= 1;
    public static bool HasMultiplayer(int scope) => scope >= 1;
    public static int DefaultCreepSlots(int scope) => scope switch { 0 => 1, 1 => 2, 2 => 2, 3 => 3, _ => 3 };

    /// <summary>The slots generated for a scope, in generation order.</summary>
    public static IReadOnlyList<SlotDef> Plan(int scope)
    {
        scope = Math.Clamp(scope, 0, 4);
        var slots = new List<SlotDef>
        {
            new("genre", "genre", "genre"),
            new("subgenre", "subgenre", "subgenre"),
            new("setting", "setting", "setting"),
            new("era", "era", "era"),
            new("protagonist", "protagonist", "protagonist"),
            new("perspective", "perspective", "perspective"),
            new("objective", "objective", "objective"),
            new("loop", "loop", "loop"),
            new("mechanic", "mechanic", "mechanic"),
        };
        for (int i = 0; i < SecondaryCount(scope); i++)
            slots.Add(new($"secondary#{i}", "secondary", "mechanic"));
        if (HasProgression(scope)) slots.Add(new("progression", "progression", "progression"));
        slots.Add(new("threat", "threat", "threat"));
        slots.Add(new("resource", "resource", "resource"));
        slots.Add(new("mood", "mood", "mood"));
        slots.Add(new("visual", "visual", "visual"));
        slots.Add(new("twist", "twist", "twist"));
        if (HasMultiplayer(scope)) slots.Add(new("multiplayer", "multiplayer", "multiplayer"));
        return slots;
    }
}
