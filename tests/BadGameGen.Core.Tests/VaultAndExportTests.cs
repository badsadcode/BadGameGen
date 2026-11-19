using BadGameGen.Core.Content;
using BadGameGen.Core.Export;
using BadGameGen.Core.Generation;
using BadGameGen.Core.Vault;

namespace BadGameGen.Core.Tests;

public sealed class VaultAndExportTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("bgg-vault");
    private string VaultPath => Path.Combine(_dir.FullName, "vault.json");
    public void Dispose() => _dir.Delete(true);

    private static Concept MakeConcept(string seed = "v", bool creep = true) =>
        new Generator(new ContentLibrary([TestContent.Synthetic()])).Generate(new Recipe { Seed = seed, Scope = 2, FeatureCreep = creep });

    [Fact]
    public void SavedIdeas_SurviveReopening_Intact()
    {
        var concept = MakeConcept();
        var saved = new VaultStore(VaultPath).Save(concept, "my notes", ["radio"]);
        var reopened = new VaultStore(VaultPath).Get(saved.Id)!;
        Assert.Equal(TestContent.Serialize(concept), TestContent.Serialize(reopened.Concept));
        Assert.Equal("my notes", reopened.Notes);
        Assert.Equal(["radio"], reopened.Tags);
    }

    [Fact]
    public void SavedIdeas_StayReadable_WhenLibraryChanges()
    {
        var concept = MakeConcept();
        var id = new VaultStore(VaultPath).Save(concept).Id;
        // Library is gone entirely; the snapshot still has every word.
        var entry = new VaultStore(VaultPath).Get(id)!;
        Assert.Equal(concept.Pitch, entry.Concept.Pitch);
        Assert.Contains(concept.Title, ConceptExporter.Markdown(entry.Concept));
    }

    [Fact]
    public void Update_Duplicate_Delete()
    {
        var clock = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        var vault = new VaultStore(VaultPath, () => clock = clock.AddMinutes(1));
        var entry = vault.Save(MakeConcept());
        var updated = vault.Update(entry.Id, new VaultStore.Patch(Title: "Crab Office", Favorite: true, Notes: "pinch", Tags: ["crabs", " crabs ", ""]));
        Assert.Equal("Crab Office", updated.Title);
        Assert.True(updated.Favorite);
        Assert.Equal(["crabs"], updated.Tags);
        Assert.True(updated.UpdatedAt > entry.CreatedAt);

        var copy = vault.Duplicate(entry.Id);
        Assert.NotEqual(entry.Id, copy.Id);
        Assert.Equal("Crab Office (copy)", copy.Title);
        Assert.False(copy.Favorite);
        Assert.Equal(2, vault.List().Count);

        Assert.True(vault.Delete(entry.Id));
        Assert.False(vault.Delete(entry.Id));
        Assert.Single(new VaultStore(VaultPath).List());
        Assert.Throws<KeyNotFoundException>(() => vault.Update("missing", new VaultStore.Patch(Title: "x")));
    }

    [Fact]
    public void CorruptVault_IsSetAsideNotDeleted()
    {
        File.WriteAllText(VaultPath, "{ this is not json");
        var vault = new VaultStore(VaultPath);
        Assert.Empty(vault.List());
        Assert.NotNull(vault.RecoveryNotice);
        Assert.Single(Directory.GetFiles(_dir.FullName, "vault.json.corrupt-*"));
        vault.Save(MakeConcept());
        Assert.Single(new VaultStore(VaultPath).List());
    }

    [Fact]
    public void Markdown_IncludesEverythingThatMatters()
    {
        var concept = MakeConcept();
        var md = ConceptExporter.Markdown(concept, "remember the crabs");
        Assert.StartsWith($"# {concept.Title}", md);
        Assert.Contains(concept.Hook, md);
        Assert.Contains(concept.Pitch, md);
        Assert.Contains("Feature Creep", md);
        Assert.Contains("remember the crabs", md);
        Assert.Contains(concept.Codename, md);
    }

    [Fact]
    public void Export_OmitsCreepSection_WhenAllRejected()
    {
        var concept = MakeConcept();
        foreach (var c in concept.Creep) c.Status = CreepStatus.Rejected;
        Assert.DoesNotContain("Feature Creep", ConceptExporter.Markdown(concept));
        Assert.DoesNotContain("FEATURE CREEP", ConceptExporter.PlainText(concept));
        Assert.DoesNotContain("Feature Creep", ConceptExporter.Markdown(MakeConcept(creep: false)));
    }

    [Fact]
    public void FileName_IsSafe()
    {
        var concept = MakeConcept();
        Assert.Equal("Crab-Office-2.md", ConceptExporter.FileName(concept, ExportFormat.Markdown, "Crab / Office: 2?"));
    }

    [Fact]
    public void RecipeCode_RoundTrips()
    {
        var recipe = new Recipe { Seed = "frost signal", Chaos = 0.72, Scope = 3, FeatureCreep = true, Pins = new() { ["genre"] = "core:genre.survival" } };
        recipe.Creep.Add(new CreepSlot { Nonce = 2, Status = CreepStatus.Accepted });
        var code = RecipeCode.Encode(recipe, ["horror", "core"]);
        Assert.StartsWith("BGG1-", code);
        Assert.True(RecipeCode.TryDecode(code, out var payload, out _));
        Assert.Equal("frost signal", payload.Recipe.Seed);
        Assert.Equal(0.72, payload.Recipe.Chaos);
        Assert.Equal("core:genre.survival", payload.Recipe.Pins["genre"]);
        Assert.Equal(CreepStatus.Accepted, payload.Recipe.Creep[0].Status);
        Assert.Equal(["core", "horror"], payload.Packs);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("BGG1-!!!!")]
    [InlineData("BGG1-AAAA")]
    public void RecipeCode_RejectsGarbageGracefully(string code)
    {
        Assert.False(RecipeCode.TryDecode(code, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }
}
