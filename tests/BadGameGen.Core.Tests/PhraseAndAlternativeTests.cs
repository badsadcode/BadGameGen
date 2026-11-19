using BadGameGen.Core.Content;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Tests;

public class PhraseAndAlternativeTests
{
    private static readonly Generator Real = new(TestContent.BuiltInLibrary());

    [Fact]
    public void Parts_AlwaysReadExactlyLikeThePlainText()
    {
        for (int i = 0; i < 300; i++)
        {
            var c = Real.Generate(new Recipe { Seed = $"parts-{i}", Scope = i % 5, Chaos = (i % 4) / 3.0 });
            Assert.Equal(c.Hook, string.Concat(c.HookParts.Select(p => p.Text)));
            Assert.Equal(c.Pitch, string.Concat(c.PitchParts.Select(p => p.Text)));
        }
    }

    [Fact]
    public void MostPhrases_AreClickable()
    {
        int sentencesWithTags = 0, concepts = 200;
        var slots = new HashSet<string>();
        for (int i = 0; i < concepts; i++)
        {
            var c = Real.Generate(new Recipe { Seed = $"click-{i}", Scope = 2 });
            var pickSlots = c.Picks.Select(p => p.Slot).ToHashSet();
            foreach (var part in c.HookParts.Concat(c.PitchParts).Where(p => p.Slot != null))
            {
                Assert.Contains(part.Slot!, pickSlots); // tags always point at a real ingredient
                slots.Add(part.Slot!);
            }
            if (c.PitchParts.Count(p => p.Slot != null) >= 4) sentencesWithTags++;
        }
        // The plain-text fallback should be rare; if it fires a lot, click-to-edit quietly dies.
        Assert.True(sentencesWithTags >= concepts * 0.95, $"only {sentencesWithTags}/{concepts} pitches had clickable phrases");
        Assert.Contains("setting", slots);
        Assert.Contains("secondary#0", slots);
    }

    [Fact]
    public void Article_IgnoresSpanMarkers()
    {
        var marked = $"{TemplateEngine.SpanStart}{(char)TemplateEngine.SpanIdBase}apple";
        Assert.Equal("an", TemplateEngine.Article(marked));
    }

    [Fact]
    public void Alternatives_AreFreshCompatibleAndReproducible()
    {
        var recipe = new Recipe { Seed = "alts", Scope = 2, Chaos = 0.2 };
        var a = Real.Generate(recipe);
        var b = Real.Generate(recipe);
        Assert.Equal(TestContent.Serialize(a.Alternatives), TestContent.Serialize(b.Alternatives));
        var picked = a.Picks.Select(p => p.ItemId).ToHashSet();
        foreach (var pick in a.Picks)
        {
            var alts = a.Alternatives[pick.Slot];
            Assert.InRange(alts.Count, 1, 3);
            Assert.DoesNotContain(alts, x => picked.Contains(x.ItemId));     // never the current or another field's pick
            Assert.Equal(alts.Count, alts.Select(x => x.ItemId).Distinct().Count());
            Assert.DoesNotContain(alts, x => string.Equals(x.Label, pick.Label, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(alts.Count, alts.Select(x => x.Label.ToLowerInvariant()).Distinct().Count());
        }
    }

    [Fact]
    public void Alternatives_RespectExclusions()
    {
        var first = Real.Generate(new Recipe { Seed = "alt-ex", Scope = 1 });
        var banned = first.Alternatives["genre"].Select(x => x.ItemId).ToList();
        var second = Real.Generate(new Recipe { Seed = "alt-ex", Scope = 1, Exclusions = banned });
        Assert.DoesNotContain(second.Alternatives["genre"], x => banned.Contains(x.ItemId));
    }

    [Fact]
    public void ChoosingAnAlternative_ChangesOnlyThatField()
    {
        var c = Real.Generate(new Recipe { Seed = "choose", Scope = 2 });
        var alt = c.Alternatives["setting"][0];
        var r = c.Recipe.Clone();
        r.Keep = c.Picks.Where(p => p.Slot != "setting").ToDictionary(p => p.Slot, p => p.ItemId);
        r.Keep["setting"] = alt.ItemId;
        var next = Real.Generate(r);
        Assert.Equal(alt.ItemId, next.Picks.Single(p => p.Slot == "setting").ItemId);
        foreach (var p in c.Picks.Where(p => p.Slot != "setting"))
            Assert.Equal(p.ItemId, next.Picks.Single(x => x.Slot == p.Slot).ItemId);
        Assert.False(next.Picks.Single(p => p.Slot == "setting").Pinned); // chosen, not locked
    }
}
