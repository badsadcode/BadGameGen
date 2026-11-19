using BadGameGen.Core.Content;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Tests;

public class FeatureCreepTests
{
    private static readonly Generator Gen = new(new ContentLibrary([TestContent.Synthetic()]));

    [Fact]
    public void Off_ProducesNoCreep()
    {
        var c = Gen.Generate(new Recipe { Seed = "c" });
        Assert.Empty(c.Creep);
        Assert.Null(c.CreepMeter);
        Assert.Empty(c.Recipe.Creep);
    }

    [Fact]
    public void On_FillsDefaultSlotsForScope()
    {
        var c = Gen.Generate(new Recipe { Seed = "c", Scope = 3, FeatureCreep = true });
        Assert.Equal(Scopes.DefaultCreepSlots(3), c.Creep.Count);
        Assert.Equal(c.Creep.Count, c.Recipe.Creep.Count);
        Assert.NotNull(c.CreepMeter);
        Assert.All(c.Creep, x => Assert.Contains("tester", x.Pitch)); // template rendered with the concept
    }

    [Fact]
    public void Creep_DoesNotChangeTheBaseConcept()
    {
        var plain = Gen.Generate(new Recipe { Seed = "same", Scope = 2 });
        var creepy = Gen.Generate(new Recipe { Seed = "same", Scope = 2, FeatureCreep = true });
        Assert.Equal(plain.Pitch, creepy.Pitch);
        Assert.Equal(plain.Hook, creepy.Hook);
        Assert.Equal(plain.Title, creepy.Title);
    }

    [Fact]
    public void RejectingOne_KeepsTheOthersStable()
    {
        var first = Gen.Generate(new Recipe { Seed = "r", Scope = 3, FeatureCreep = true });
        var recipe = first.Recipe.Clone();
        recipe.Creep[0].Status = CreepStatus.Rejected;
        var second = Gen.Generate(recipe);
        Assert.DoesNotContain(second.Creep, x => x.Slot == 0);
        // Slot 1 and 2 keep their items unless slot 0's item was the only thing blocking a duplicate.
        Assert.Equal(first.Creep.Skip(1).Count(), second.Creep.Count);
    }

    [Fact]
    public void AndAnotherThing_AppendsWithoutDisturbing()
    {
        var first = Gen.Generate(new Recipe { Seed = "a", Scope = 1, FeatureCreep = true });
        var recipe = first.Recipe.Clone();
        recipe.Creep.Add(new CreepSlot());
        var second = Gen.Generate(recipe);
        Assert.Equal(first.Creep.Count + 1, second.Creep.Count);
        Assert.Equal(first.Creep.Select(x => x.ItemId), second.Creep.Take(first.Creep.Count).Select(x => x.ItemId));
        Assert.Equal(second.Creep.Count, second.Creep.Select(x => x.ItemId).Distinct().Count());
    }

    [Fact]
    public void RerollingASlot_ChangesOnlyThatSlot()
    {
        var first = Gen.Generate(new Recipe { Seed = "z", Scope = 3, FeatureCreep = true });
        var recipe = first.Recipe.Clone();
        bool changed = false;
        for (int n = 1; n < 6 && !changed; n++)
        {
            recipe.Creep[2].Nonce = n;
            var next = Gen.Generate(recipe);
            Assert.Equal(first.Creep[0].ItemId, next.Creep[0].ItemId);
            Assert.Equal(first.Creep[1].ItemId, next.Creep[1].ItemId);
            changed = next.Creep[2].ItemId != first.Creep[2].ItemId;
        }
        Assert.True(changed);
    }

    [Fact]
    public void KindsFilter_IsRespected()
    {
        var c = Gen.Generate(new Recipe { Seed = "k", Scope = 3, FeatureCreep = true, CreepKinds = ["whyNot"] });
        Assert.NotEmpty(c.Creep);
        Assert.All(c.Creep, x => Assert.Equal("whyNot", x.Kind));
    }

    [Fact]
    public void Meter_GrowsWithAcceptedFeatures()
    {
        var c = Gen.Generate(new Recipe { Seed = "m", Scope = 2, FeatureCreep = true });
        var recipe = c.Recipe.Clone();
        foreach (var slot in recipe.Creep) slot.Status = CreepStatus.Accepted;
        var accepted = Gen.Generate(recipe);
        Assert.True(accepted.CreepMeter!.Score > c.CreepMeter!.Score);
        foreach (var slot in recipe.Creep) slot.Status = CreepStatus.Rejected;
        Assert.True(Gen.Generate(recipe).CreepMeter!.Score < c.CreepMeter.Score);
    }
}
