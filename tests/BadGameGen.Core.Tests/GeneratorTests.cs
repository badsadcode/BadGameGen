using BadGameGen.Core.Content;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Tests;

public class GeneratorTests
{
    private static Generator Synth(int perCategory = 8) => new(new ContentLibrary([TestContent.Synthetic(perCategory: perCategory)]));

    private static Dictionary<string, string> PickMap(Concept c) => c.Picks.ToDictionary(p => p.Slot, p => p.ItemId);

    [Fact]
    public void SameRecipe_ProducesIdenticalConcept()
    {
        var gen = new Generator(TestContent.BuiltInLibrary());
        var recipe = new Recipe { Seed = "418920", Chaos = 0.72, Scope = 2, FeatureCreep = true };
        var a = TestContent.Serialize(gen.Generate(recipe));
        var b = TestContent.Serialize(new Generator(TestContent.BuiltInLibrary()).Generate(recipe));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Seed_IsCaseAndWhitespaceInsensitive()
    {
        var gen = Synth();
        Assert.Equal(PickMap(gen.Generate(new Recipe { Seed = "Frost" })), PickMap(gen.Generate(new Recipe { Seed = "  frost " })));
    }

    [Fact]
    public void DifferentSeeds_ProduceVariedConcepts()
    {
        var gen = new Generator(TestContent.BuiltInLibrary());
        var titles = Enumerable.Range(0, 60).Select(i => gen.Generate(new Recipe { Seed = $"s{i}" }).Title).ToHashSet();
        var hooks = Enumerable.Range(0, 60).Select(i => gen.Generate(new Recipe { Seed = $"s{i}" }).Hook).ToHashSet();
        Assert.True(titles.Count >= 45, $"only {titles.Count} distinct titles");
        Assert.True(hooks.Count >= 58, $"only {hooks.Count} distinct hooks");
    }

    [Fact]
    public void PackOrder_DoesNotChangeResults()
    {
        var a = TestContent.Synthetic("alpha");
        var b = TestContent.Synthetic("beta");
        var one = new Generator(new ContentLibrary([a, b])).Generate(new Recipe { Seed = "x" });
        var two = new Generator(new ContentLibrary([b, a])).Generate(new Recipe { Seed = "x" });
        Assert.Equal(TestContent.Serialize(one), TestContent.Serialize(two));
    }

    [Fact]
    public void PinnedFields_SurviveNewSeeds()
    {
        var gen = Synth();
        var first = gen.Generate(new Recipe { Seed = "1", Scope = 2 });
        var pins = first.Picks.Where(p => p.Slot is "genre" or "setting" or "secondary#1").ToDictionary(p => p.Slot, p => p.ItemId);
        for (int i = 0; i < 20; i++)
        {
            var next = gen.Generate(new Recipe { Seed = $"other-{i}", Scope = 2, Pins = pins });
            var map = PickMap(next);
            foreach (var (slot, id) in pins) Assert.Equal(id, map[slot]);
            Assert.All(next.Picks.Where(p => pins.ContainsKey(p.Slot)), p => Assert.True(p.Pinned));
        }
    }

    [Fact]
    public void RerollingOneField_LeavesOthersUntouched()
    {
        var gen = Synth();
        var baseRecipe = new Recipe { Seed = "abc", Scope = 3 };
        var before = PickMap(gen.Generate(baseRecipe));
        var rerolled = baseRecipe.Clone();
        rerolled.Rerolls["twist"] = 1;
        var after = PickMap(gen.Generate(rerolled));
        foreach (var slot in before.Keys.Where(k => k != "twist")) Assert.Equal(before[slot], after[slot]);

        // And a few rerolls must eventually change the twist itself.
        var changed = Enumerable.Range(1, 6).Any(n =>
        {
            var r = baseRecipe.Clone();
            r.Rerolls["twist"] = n;
            return PickMap(gen.Generate(r))["twist"] != before["twist"];
        });
        Assert.True(changed);
    }

    [Fact]
    public void RerollWithKeep_ChangesExactlyOneFieldOnRealContent()
    {
        var gen = new Generator(TestContent.BuiltInLibrary());
        for (int i = 0; i < 30; i++)
        {
            var recipe = new Recipe { Seed = $"keep-{i}", Scope = 3 };
            var before = PickMap(gen.Generate(recipe));
            recipe.Keep = before.Where(kv => kv.Key != "setting").ToDictionary();
            recipe.Rerolls["setting"] = 1;
            var after = PickMap(gen.Generate(recipe));
            foreach (var slot in before.Keys.Where(k => k != "setting")) Assert.Equal(before[slot], after[slot]);
        }
    }

    /// <summary>Even without Keep, race sampling keeps most fields put when a neighbour changes.</summary>
    [Fact]
    public void RerollingOneField_IsMostlyStableOnRealContent()
    {
        var gen = new Generator(TestContent.BuiltInLibrary());
        int stable = 0, total = 0;
        for (int i = 0; i < 30; i++)
        {
            var recipe = new Recipe { Seed = $"real-{i}", Scope = 2 };
            var before = PickMap(gen.Generate(recipe));
            recipe.Rerolls["setting"] = 1;
            var after = PickMap(gen.Generate(recipe));
            foreach (var slot in before.Keys.Where(k => k != "setting"))
            {
                total++;
                if (after.TryGetValue(slot, out var v) && v == before[slot]) stable++;
            }
        }
        Assert.True(stable >= total * 0.75, $"only {stable}/{total} fields stayed put");
    }

    [Fact]
    public void Exclusions_AreNeverPicked()
    {
        var gen = Synth(perCategory: 3);
        var excluded = new List<string> { "synth:genre.genre-0", "synth:genre.genre-1" };
        for (int i = 0; i < 30; i++)
            Assert.Equal("synth:genre.genre-2", PickMap(gen.Generate(new Recipe { Seed = $"e{i}", Exclusions = excluded }))["genre"]);
    }

    [Theory]
    [InlineData(0, 0, false, false)]
    [InlineData(1, 1, true, true)]
    [InlineData(2, 2, true, true)]
    [InlineData(4, 4, true, true)]
    public void Scope_ControlsSystemCount(int scope, int secondaries, bool progression, bool multiplayer)
    {
        var c = Synth().Generate(new Recipe { Seed = "scope", Scope = scope });
        Assert.Equal(secondaries, c.Picks.Count(p => p.Field == "secondary"));
        Assert.Equal(progression, c.Picks.Any(p => p.Field == "progression"));
        Assert.Equal(multiplayer, c.Picks.Any(p => p.Field == "multiplayer"));
        Assert.Equal(Scopes.Names[scope], c.ScopeName);
    }

    [Fact]
    public void SecondaryMechanics_NeverDuplicateTheCoreMechanic()
    {
        var gen = Synth(perCategory: 6);
        for (int i = 0; i < 40; i++)
        {
            var mechanics = gen.Generate(new Recipe { Seed = $"m{i}", Scope = 4 }).Picks.Where(p => p.Category == "mechanic").Select(p => p.ItemId).ToList();
            Assert.Equal(mechanics.Count, mechanics.Distinct().Count());
        }
    }

    private static ContentLibrary ConflictLibrary()
    {
        var pack = TestContent.Synthetic(perCategory: 1);
        pack.Items["setting"] = [
            new ContentItem { Id = "space", Label = "Space", Tags = ["space"], Forms = new() { ["noun"] = "space station", ["in"] = "on a space station" } },
        ];
        pack.Items["threat"] = [
            new ContentItem { Id = "drowning", Label = "Drowning", Tags = ["water"], Excludes = ["space"], Weight = 50, Forms = new() { ["noun"] = "the tide", ["clause"] = "the tide comes in" } },
            new ContentItem { Id = "vacuum", Label = "Vacuum", Tags = ["space"], Weight = 1, Forms = new() { ["noun"] = "vacuum", ["clause"] = "the air leaks out" } },
        ];
        var errors = new List<string>();
        PackLoader.Validate(pack, errors, []);
        Assert.Empty(errors);
        return new ContentLibrary([pack]);
    }

    [Fact]
    public void LowChaos_RespectsExclusions()
    {
        var gen = new Generator(ConflictLibrary());
        for (int i = 0; i < 50; i++)
            Assert.Equal("synth:threat.vacuum", PickMap(gen.Generate(new Recipe { Seed = $"c{i}", Chaos = 0.2 }))["threat"]);
    }

    [Fact]
    public void HighChaos_AllowsAudaciousCombinations()
    {
        var gen = new Generator(ConflictLibrary());
        var threats = Enumerable.Range(0, 50).Select(i => PickMap(gen.Generate(new Recipe { Seed = $"c{i}", Chaos = 1.0 }))["threat"]).ToHashSet();
        Assert.Contains("synth:threat.drowning", threats);
    }

    [Fact]
    public void Chaos_ShiftsTowardsWeirderItems()
    {
        var lib = TestContent.BuiltInLibrary();
        var gen = new Generator(lib);
        double AvgWeirdness(double chaos) => Enumerable.Range(0, 120)
            .SelectMany(i => gen.Generate(new Recipe { Seed = $"w{i}", Chaos = chaos }).Picks)
            .Select(p => lib.TryGetItem(p.ItemId, out var item) ? item.Weirdness : 0).Average();
        Assert.True(AvgWeirdness(1.0) > AvgWeirdness(0.0) + 0.1);
    }

    [Fact]
    public void Silliness_ShiftsTowardsSillierItems()
    {
        var lib = TestContent.BuiltInLibrary();
        var gen = new Generator(lib);
        double Avg(double silly) => Enumerable.Range(0, 120)
            .SelectMany(i => gen.Generate(new Recipe { Seed = $"y{i}", Silliness = silly }).Picks)
            .Select(p => lib.TryGetItem(p.ItemId, out var item) ? item.Silliness : 0).Average();
        Assert.True(Avg(1.0) > Avg(0.0) + 0.1);
    }

    [Fact]
    public void MissingPin_FallsBackWithWarning()
    {
        var c = Synth().Generate(new Recipe { Seed = "p", Pins = new() { ["genre"] = "gone:genre.nope" } });
        Assert.Contains(c.Warnings, w => w.Contains("gone:genre.nope"));
        Assert.Contains(c.Picks, p => p.Slot == "genre");
    }

    [Fact]
    public void TitleOverride_IsUsedVerbatim()
    {
        var c = Synth().Generate(new Recipe { Seed = "t", TitleOverride = "Crab Office" });
        Assert.Equal("Crab Office", c.Title);
    }

    [Fact]
    public void Codename_ReflectsSeedScopeAndCreep()
    {
        var gen = Synth();
        Assert.Equal("BG-418920-M", gen.Generate(new Recipe { Seed = "418920", Scope = 2 }).Codename);
        Assert.Equal("BG-000042-SC", gen.Generate(new Recipe { Seed = "42", Scope = 1, FeatureCreep = true }).Codename);
        Assert.Matches(@"^BG-\d{6}-T$", gen.Generate(new Recipe { Seed = "frost signal", Scope = 0 }).Codename);
    }

    [Fact]
    public void Normalize_ClampsOutOfRangeValues()
    {
        var r = Generator.Normalize(new Recipe { Seed = " ", Chaos = 7, Silliness = double.NaN, Scope = 99 });
        Assert.Equal("0", r.Seed);
        Assert.Equal(1, r.Chaos);
        Assert.Equal(0.5, r.Silliness);
        Assert.Equal(4, r.Scope);
    }
}
