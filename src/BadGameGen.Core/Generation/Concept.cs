using BadGameGen.Core.Content;

namespace BadGameGen.Core.Generation;

/// <summary>
/// A fully rendered game concept. It is a self-contained snapshot: all text is stored, so a saved
/// concept stays readable even if the packs it came from change or disappear.
/// </summary>
public sealed class Concept
{
    public string GeneratorVersion { get; set; } = "";
    public string Codename { get; set; } = "";
    public string Title { get; set; } = "";
    public string Hook { get; set; } = "";
    public string Pitch { get; set; } = "";
    /// <summary>Hook split into parts tagged with the slot that produced them (click-to-edit).</summary>
    public List<TextPart> HookParts { get; set; } = new();
    /// <summary>Pitch split into parts tagged with the slot that produced them (click-to-edit).</summary>
    public List<TextPart> PitchParts { get; set; } = new();
    /// <summary>Slot → a few good alternatives that fit the rest of the concept.</summary>
    public Dictionary<string, List<AltOption>> Alternatives { get; set; } = new();
    public List<ConceptSection> Expanded { get; set; } = new();
    public string BadIdea { get; set; } = "";
    public string MightWork { get; set; } = "";
    public List<ConceptPick> Picks { get; set; } = new();
    public List<ConceptChip> Chips { get; set; } = new();
    public List<CreepProposal> Creep { get; set; } = new();
    public CreepMeter? CreepMeter { get; set; }
    public string ScopeName { get; set; } = "";
    public Recipe Recipe { get; set; } = new();
    public List<PackRef> Library { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public sealed record AltOption(string ItemId, string Label);

public sealed class ConceptSection
{
    public string Key { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class ConceptPick
{
    public string Slot { get; set; } = "";
    public string Field { get; set; } = "";
    public string Category { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string Label { get; set; } = "";
    public string PackId { get; set; } = "";
    public bool Pinned { get; set; }
}

public sealed class ConceptChip
{
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "";
}

public sealed class CreepProposal
{
    public int Slot { get; set; }
    public string ItemId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string KindLabel { get; set; } = "";
    public string Name { get; set; } = "";
    public string Pitch { get; set; } = "";
    /// <summary>Hook split into parts tagged with the slot that produced them (click-to-edit).</summary>
    public List<TextPart> HookParts { get; set; } = new();
    /// <summary>Pitch split into parts tagged with the slot that produced them (click-to-edit).</summary>
    public List<TextPart> PitchParts { get; set; } = new();
    /// <summary>Slot → a few good alternatives that fit the rest of the concept.</summary>
    public Dictionary<string, List<AltOption>> Alternatives { get; set; } = new();
    public string? Connects { get; set; }
    public int Complexity { get; set; }
    public string Status { get; set; } = CreepStatus.Proposed;
}

public sealed class CreepMeter
{
    public int Score { get; set; }
    public string Label { get; set; } = "";
    public string? Warning { get; set; }
}
