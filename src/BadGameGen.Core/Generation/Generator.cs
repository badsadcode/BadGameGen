using System.Globalization;
using BadGameGen.Core.Content;
using BadGameGen.Core.Randomness;

namespace BadGameGen.Core.Generation;

/// <summary>
/// Turns a <see cref="Recipe"/> plus a <see cref="ContentLibrary"/> into a <see cref="Concept"/>.
/// Pure and deterministic: same recipe + same library = same concept, every time.
/// </summary>
public sealed class Generator
{
    /// <summary>
    /// Bump when an algorithm change alters output for an existing recipe, so saved ideas can
    /// say "generated with an older generator" instead of silently differing.
    /// </summary>
    public const string Version = "1";

    private readonly ContentLibrary _lib;

    public Generator(ContentLibrary library) => _lib = library;

    public Concept Generate(Recipe input)
    {
        var recipe = Normalize(input);
        var warnings = new List<string>();
        var slots = Scopes.Plan(recipe.Scope);
        var excluded = new HashSet<string>(recipe.Exclusions);

        // Pass 1: place pinned (and kept) items first so unlocked picks can respect their compatibility rules.
        var picks = new Dictionary<string, (ContentItem Item, bool Pinned)>();
        foreach (var slot in slots)
        {
            if (!recipe.Pins.TryGetValue(slot.Key, out var pinId)) continue;
            if (_lib.TryGetItem(pinId, out var pinned) && pinned.Category == slot.Category)
                picks[slot.Key] = (pinned, true);
            else
                warnings.Add($"Locked {slot.Field} '{pinId}' is not in the enabled packs, so it was rerolled.");
        }
        foreach (var slot in slots)
        {
            if (picks.ContainsKey(slot.Key) || !recipe.Keep.TryGetValue(slot.Key, out var keepId)) continue;
            if (_lib.TryGetItem(keepId, out var kept) && kept.Category == slot.Category && !excluded.Contains(keepId))
                picks[slot.Key] = (kept, false);
        }

        // Pass 2: fill the rest in a fixed order.
        foreach (var slot in slots)
        {
            if (picks.ContainsKey(slot.Key)) continue;
            var placed = picks.Values.Select(p => p.Item).ToList();
            var candidates = _lib.Items(slot.Category)
                .Where(i => !excluded.Contains(i.FullId) && !placed.Contains(i))
                .ToList();
            var item = PickItem(candidates, placed, recipe, slot.Key, recipe.RerollCount(slot.Key));
            if (item == null)
            {
                warnings.Add($"No usable {slot.Category} items in the enabled packs.");
                continue;
            }
            picks[slot.Key] = (item, false);
        }

        // Build the template context in slot order.
        var ctx = new TemplateContext();
        foreach (var slot in slots)
            if (picks.TryGetValue(slot.Key, out var p))
                ctx.Add(slot.Field, p.Item, slot.Key);

        var conceptTags = new HashSet<string>(picks.Values.SelectMany(p => p.Item.Tags));
        // Themed templates need the theme to be a real presence (2+ picks), not a single stray item.
        var themeTags = picks.Values.SelectMany(p => p.Item.Tags.Distinct())
            .GroupBy(t => t).Where(g => g.Count() >= 2).Select(g => g.Key).ToHashSet();
        int wording = recipe.RerollCount("wording");

        // Title
        string title;
        if (!string.IsNullOrWhiteSpace(recipe.TitleOverride))
        {
            title = recipe.TitleOverride.Trim();
        }
        else
        {
            int titleNonce = recipe.RerollCount("title");
            var first = PickTitlePart(Categories.TitleFirst, conceptTags, recipe, "title:first", titleNonce, null);
            var last = PickTitlePart(Categories.TitleLast, conceptTags, recipe, "title:last", titleNonce, first);
            if (first != null) ctx.Set("first", first);
            if (last != null) ctx.Set("last", last);
            var titleText = RenderSection(TemplateSections.Title, ctx, recipe, themeTags, titleNonce);
            title = TemplateEngine.TitleCase(titleText ?? $"{first?.Label} {last?.Label}".Trim());
            if (string.IsNullOrWhiteSpace(title)) title = "Untitled Bad Idea";
        }
        ctx.Title = title;

        var concept = new Concept
        {
            GeneratorVersion = Version,
            Codename = MakeCodename(recipe),
            Title = title,
            ScopeName = Scopes.Names[recipe.Scope],
            Recipe = recipe,
            Library = _lib.Signature.ToList(),
            Warnings = warnings,
        };

        concept.HookParts = RenderSectionParts(TemplateSections.Hook, ctx, recipe, themeTags, wording) ?? [];
        concept.Hook = string.Concat(concept.HookParts.Select(p => p.Text));
        foreach (var section in TemplateSections.PitchOrder)
        {
            var parts = RenderSectionParts(section, ctx, recipe, themeTags, wording);
            if (parts == null || parts.Count == 0) continue;
            if (concept.PitchParts.Count > 0) concept.PitchParts.Add(new TextPart(" ", null));
            concept.PitchParts.AddRange(parts);
        }
        concept.Pitch = string.Concat(concept.PitchParts.Select(p => p.Text));
        concept.BadIdea = RenderSection(TemplateSections.BadIdea, ctx, recipe, themeTags, wording) ?? "";
        concept.MightWork = RenderSection(TemplateSections.MightWork, ctx, recipe, themeTags, wording) ?? "";
        concept.Expanded = BuildExpanded(ctx, recipe, themeTags, wording);

        foreach (var slot in slots)
        {
            if (!picks.TryGetValue(slot.Key, out var p)) continue;
            concept.Picks.Add(new ConceptPick
            {
                Slot = slot.Key,
                Field = slot.Field,
                Category = slot.Category,
                ItemId = p.Item.FullId,
                Label = p.Item.Label,
                PackId = p.Item.PackId,
                Pinned = p.Pinned,
            });
        }
        concept.Chips = BuildChips(picks, recipe);
        concept.Alternatives = BuildAlternatives(slots, picks, recipe, excluded);

        if (recipe.FeatureCreep)
        {
            concept.Creep = BuildCreep(recipe, ctx, conceptTags);
            concept.CreepMeter = BuildMeter(recipe, concept.Creep);
        }
        return concept;
    }

    /// <summary>Clamps values and fills defaults so the stored recipe is always canonical.</summary>
    public static Recipe Normalize(Recipe input)
    {
        var r = input.Clone();
        r.Seed = string.IsNullOrWhiteSpace(r.Seed) ? "0" : r.Seed.Trim();
        r.Chaos = Math.Round(Math.Clamp(double.IsFinite(r.Chaos) ? r.Chaos : 0.35, 0, 1), 3);
        r.Silliness = Math.Round(Math.Clamp(double.IsFinite(r.Silliness) ? r.Silliness : 0.5, 0, 1), 3);
        r.Scope = Math.Clamp(r.Scope, 0, 4);
        r.CreepKinds = r.CreepKinds.Where(Categories.CreepKinds.Contains).Distinct().ToList();
        foreach (var slot in r.Creep)
            if (!CreepStatus.All.Contains(slot.Status)) slot.Status = CreepStatus.Proposed;
        if (r.FeatureCreep && r.Creep.Count == 0)
            for (int i = 0; i < Scopes.DefaultCreepSlots(r.Scope); i++) r.Creep.Add(new CreepSlot());
        return r;
    }

    // ---------------------------------------------------------------- weighting

    /// <summary>
    /// How much an item wants to be picked, given what's already in the concept and the sliders.
    /// Kept in one place so the "personality" of the generator is easy to tune.
    /// </summary>
    internal static double Score(ContentItem item, IReadOnlyList<ContentItem> placed, Recipe r)
    {
        double chaos = r.Chaos;
        double w = item.Weight;

        // Chaos: grounded favours low weirdness, audacious favours high weirdness.
        double grounded = Math.Pow(1 - item.Weirdness, 2) + 0.05;
        double audacious = item.Weirdness + 0.25;
        w *= grounded + (audacious - grounded) * chaos;

        // Serious <-> silly.
        w *= Math.Max(0.08, 1 - 0.9 * Math.Abs(item.Silliness - r.Silliness));

        // Scope affinity.
        if (item.Scope is [var min, var max] && (r.Scope < min || r.Scope > max))
            w *= chaos >= 0.75 ? 0.3 : 0;

        // Compatibility: conflicts are forbidden until chaos gets high, then merely unlikely.
        bool conflict = placed.Any(p =>
            p.Tags.Any(item.Excludes.Contains) || item.Tags.Any(p.Excludes.Contains));
        if (conflict)
            w *= chaos < 0.7 ? 0 : (chaos - 0.7) / 0.3 * 0.6;

        // Affinity: coherent combinations get a boost, which fades as chaos rises.
        int matches = placed.Count(p => p.Tags.Any(item.Affinity.Contains) || item.Tags.Any(p.Affinity.Contains));
        w *= 1 + matches * 1.5 * (1 - chaos * 0.8);

        return w;
    }

    private static ContentItem? PickItem(List<ContentItem> candidates, IReadOnlyList<ContentItem> placed, Recipe r, string stream, int nonce)
    {
        if (candidates.Count == 0) return null;
        var weights = candidates.Select(c => Score(c, placed, r)).ToList();
        int index = SeedRng.Race(weights, i => candidates[i].FullId, r.Seed, stream, nonce);
        if (index < 0)
        {
            // Everything conflicts: fall back to plain weights rather than leaving a hole.
            index = SeedRng.Race(candidates.Select(c => c.Weight).ToList(), i => candidates[i].FullId, r.Seed, stream, nonce);
        }
        return index < 0 ? null : candidates[index];
    }

    private ContentItem? PickTitlePart(string category, HashSet<string> conceptTags, Recipe r, string stream, int nonce, ContentItem? avoid)
    {
        // Avoid titles that repeat a word, e.g. "Static Static Noise".
        var avoidWords = avoid?.Label.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToLowerInvariant()).ToHashSet() ?? [];
        var candidates = _lib.Items(category)
            .Where(i => i != avoid && !i.Label.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => avoidWords.Contains(w.ToLowerInvariant())))
            .ToList();
        if (candidates.Count == 0) return null;
        double coherence = 3.0 * (1 - r.Chaos * 0.7);
        var weights = candidates.Select(c =>
        {
            double w = c.Weight * Math.Max(0.08, 1 - 0.9 * Math.Abs(c.Silliness - r.Silliness));
            int overlap = c.Tags.Count(conceptTags.Contains) + c.Affinity.Count(conceptTags.Contains);
            return w * (1 + overlap * coherence);
        }).ToList();
        int index = SeedRng.Race(weights, i => candidates[i].FullId, r.Seed, stream, nonce);
        return index < 0 ? null : candidates[index];
    }

    // ---------------------------------------------------------------- templates

    private string? RenderSection(string section, TemplateContext ctx, Recipe r, HashSet<string> tags, int nonce)
    {
        var template = ChooseTemplate(section, ctx, r, tags, nonce);
        return template == null ? null : TemplateEngine.Render(template.Text, ctx);
    }

    private List<TextPart>? RenderSectionParts(string section, TemplateContext ctx, Recipe r, HashSet<string> tags, int nonce)
    {
        var template = ChooseTemplate(section, ctx, r, tags, nonce);
        return template == null ? null : TemplateEngine.RenderParts(template.Text, ctx);
    }

    private TemplateDef? ChooseTemplate(string section, TemplateContext ctx, Recipe r, HashSet<string> tags, int nonce)
    {
        var eligible = _lib.Templates(section)
            .Where(t => (t.Scopes == null || t.Scopes.Contains(r.Scope))
                        && (t.TagsAny == null || t.TagsAny.Count == 0 || t.TagsAny.Any(tags.Contains))
                        && TemplateEngine.CanRender(t.Text, ctx))
            .ToList();
        if (eligible.Count == 0) return null;
        // Tag-specific templates are more interesting, so they get a boost when eligible.
        var weights = eligible.Select(t => t.Weight * (t.TagsAny is { Count: > 0 } ? 2.0 : 1.0)).ToList();
        int index = SeedRng.Race(weights, i => eligible[i].FullId, r.Seed, "tpl:" + section, nonce);
        return eligible[Math.Max(index, 0)];
    }

    /// <summary>
    /// For each slot, a few alternatives that fit the rest of the concept (same scoring as
    /// generation, ranked by a seeded race so they're reproducible). Shown as one-click swaps.
    /// </summary>
    private Dictionary<string, List<AltOption>> BuildAlternatives(
        IReadOnlyList<SlotDef> slots, Dictionary<string, (ContentItem Item, bool Pinned)> picks, Recipe r, HashSet<string> excluded)
    {
        const int Count = 3;
        var result = new Dictionary<string, List<AltOption>>();
        var used = picks.Values.Select(p => p.Item).ToHashSet();
        foreach (var slot in slots)
        {
            if (!picks.TryGetValue(slot.Key, out var current)) continue;
            var placed = picks.Where(kv => kv.Key != slot.Key).Select(kv => kv.Value.Item).ToList();
            var candidates = _lib.Items(slot.Category).Where(i => !used.Contains(i) && !excluded.Contains(i.FullId)).ToList();
            var weights = candidates.Select(c => Score(c, placed, r)).ToList();
            var order = SeedRng.RaceOrder(weights, i => candidates[i].FullId, r.Seed, "alt:" + slot.Key, r.RerollCount(slot.Key));
            // Different packs can have items with the same name; never offer "X instead of X".
            var seenLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current.Item.Label };
            result[slot.Key] = order.Select(i => candidates[i]).Where(c => seenLabels.Add(c.Label))
                .Take(Count).Select(c => new AltOption(c.FullId, c.Label)).ToList();
        }
        return result;
    }

    private List<ConceptSection> BuildExpanded(TemplateContext ctx, Recipe r, HashSet<string> tags, int wording)
    {
        var sections = new List<ConceptSection>();
        void Add(string key, string heading, string template)
        {
            if (!TemplateEngine.CanRender(template, ctx)) return;
            sections.Add(new ConceptSection { Key = key, Heading = heading, Body = TemplateEngine.Render(template, ctx) });
        }

        Add("role", "Who you are", "You play [a] {protagonist.noun} {setting.in} {era.in}. Your goal: {objective.verb}.");
        Add("loop", "Core loop", "You {loop.verb}.");
        Add("mechanic", "Core mechanic", "{mechanic}: you {mechanic.verb}.");
        if (ctx.Has("secondary"))
        {
            // One line per supporting system: "Crafting: you bolt junk together."
            var lines = ctx.GetAll("secondary").Select(item =>
            {
                var one = new TemplateContext();
                one.Set("mechanic", item);
                return TemplateEngine.Render("{mechanic}: you {mechanic.verb}.", one);
            });
            sections.Add(new ConceptSection { Key = "systems", Heading = "Supporting systems", Body = string.Join("\n", lines) });
        }
        Add("progression", "Progression", "You {progression.verb}.");
        Add("threat", "Opposition", "{threat.clause}.");
        Add("resource", "Key resource", "Everything runs on {resource.noun}.");
        Add("look", "Look and feel", "Presented in {visual.noun}, seen from [a] {perspective.noun}. The tone is {mood.adj}.");
        Add("players", "Players", "{multiplayer.phrase}.");
        Add("twist", "The twist", "{twist.clause}.");
        var scopeNote = RenderSection(TemplateSections.ScopeNote, ctx, r, tags, wording);
        if (!string.IsNullOrWhiteSpace(scopeNote))
            sections.Add(new ConceptSection { Key = "scope", Heading = $"Scope check: {Scopes.Names[r.Scope]}", Body = scopeNote });
        return sections;
    }

    private static List<ConceptChip> BuildChips(Dictionary<string, (ContentItem Item, bool Pinned)> picks, Recipe r)
    {
        var chips = new List<ConceptChip>();
        foreach (var key in new[] { "genre", "subgenre", "setting", "mood", "multiplayer" })
            if (picks.TryGetValue(key, out var p))
                chips.Add(new ConceptChip { Label = p.Item.Label, Kind = key });
        if (r.Chaos >= 0.7) chips.Add(new ConceptChip { Label = "Weird", Kind = "chaos" });
        else if (r.Chaos <= 0.2) chips.Add(new ConceptChip { Label = "Grounded", Kind = "chaos" });
        return chips;
    }

    private static string MakeCodename(Recipe r)
    {
        var normalized = SeedRng.NormalizeSeed(r.Seed);
        string digits = normalized.Length is > 0 and <= 6 && normalized.All(char.IsDigit)
            ? normalized.PadLeft(6, '0')
            : (StableHash.Fnv1a64(normalized) % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        return $"BG-{digits}-{Scopes.Letters[r.Scope]}{(r.FeatureCreep ? "C" : "")}";
    }

    // ---------------------------------------------------------------- feature creep

    private static readonly Dictionary<string, string> CreepKindLabels = new()
    {
        ["system"] = "Side system",
        ["loop"] = "Extra loop",
        ["content"] = "More content",
        ["whyNot"] = "Why not?",
    };

    private List<CreepProposal> BuildCreep(Recipe r, TemplateContext ctx, HashSet<string> conceptTags)
    {
        var proposals = new List<CreepProposal>();
        var used = new HashSet<ContentItem>();
        var pool = _lib.Items(Categories.Creep)
            .Where(i => r.CreepKinds.Contains(i.Kind ?? "")
                        && !r.Exclusions.Contains(i.FullId)
                        && TemplateEngine.CanRender(i.Form("pitch"), ctx)
                        && (!i.Forms.ContainsKey("connects") || TemplateEngine.CanRender(i.Forms["connects"], ctx)))
            .ToList();

        for (int slotIndex = 0; slotIndex < r.Creep.Count; slotIndex++)
        {
            var slot = r.Creep[slotIndex];
            if (slot.Status == CreepStatus.Rejected) continue;
            var candidates = pool.Where(i => !used.Contains(i)).ToList();
            if (candidates.Count == 0) break;
            var weights = candidates.Select(c =>
            {
                double w = c.Weight;
                double grounded = Math.Pow(1 - c.Weirdness, 2) + 0.05;
                w *= grounded + (c.Weirdness + 0.25 - grounded) * r.Chaos;
                w *= Math.Max(0.08, 1 - 0.9 * Math.Abs(c.Silliness - r.Silliness));
                int overlap = c.Affinity.Count(conceptTags.Contains) + c.Tags.Count(conceptTags.Contains);
                w *= 1 + overlap * 1.2;
                if (c.Excludes.Any(conceptTags.Contains)) w *= r.Chaos < 0.7 ? 0.02 : 0.4;
                return w;
            }).ToList();
            int index = SeedRng.Race(weights, i => candidates[i].FullId, r.Seed, $"creep#{slotIndex}", slot.Nonce);
            if (index < 0) index = SeedRng.ForStream(r.Seed, $"creep#{slotIndex}", slot.Nonce).NextInt(candidates.Count);
            var item = candidates[index];
            used.Add(item);
            proposals.Add(new CreepProposal
            {
                Slot = slotIndex,
                ItemId = item.FullId,
                Kind = item.Kind ?? "",
                KindLabel = CreepKindLabels.GetValueOrDefault(item.Kind ?? "", "Extra"),
                Name = item.Label,
                Pitch = TemplateEngine.Render(item.Form("pitch"), ctx),
                Connects = item.Forms.TryGetValue("connects", out var c) && !string.IsNullOrWhiteSpace(c)
                    ? TemplateEngine.Render(c, ctx) : null,
                Complexity = item.Complexity,
                Status = slot.Status,
            });
        }
        return proposals;
    }

    private static readonly (int Max, string Label)[] MeterLabels =
    [
        (20, "Totally reasonable"),
        (35, "Weekend-ish"),
        (50, "Mild creep"),
        (65, "Ambitious"),
        (80, "Stretch goals unlocked"),
        (100, "Five-year roadmap"),
        (int.MaxValue, "Sentient scope"),
    ];

    private static readonly int[] ScopeBase = [8, 18, 32, 50, 72];

    public static CreepMeter BuildMeter(Recipe r, IEnumerable<CreepProposal> creep)
    {
        int score = ScopeBase[Math.Clamp(r.Scope, 0, 4)];
        foreach (var c in creep)
            score += c.Status == CreepStatus.Accepted ? c.Complexity * 5 : c.Status == CreepStatus.Proposed ? c.Complexity * 3 : 0;
        return new CreepMeter
        {
            Score = score,
            Label = MeterLabels.First(m => score < m.Max).Label,
            Warning = score >= 100 ? "Scope warning: this is no longer a game, it's a lifestyle."
                : score >= 80 ? "Scope warning: consider cutting something. Anything. Please."
                : null,
        };
    }
}
