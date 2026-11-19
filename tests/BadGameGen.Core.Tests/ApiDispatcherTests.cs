using System.Text.Json;
using BadGameGen.App;

namespace BadGameGen.Core.Tests;

public sealed class ApiDispatcherTests : IDisposable
{
    private readonly DirectoryInfo _data = Directory.CreateTempSubdirectory("bgg-app");
    private readonly ApiDispatcher _api;

    public ApiDispatcherTests()
    {
        var service = new BadGameGenService(new AppPaths(Path.Combine(TestContent.RepoRoot, "content"), _data.FullName));
        _api = new ApiDispatcher(service);
    }

    public void Dispose() => _data.Delete(true);

    private JsonElement Call(string method, object? @params = null)
    {
        var response = _api.HandleJson(JsonSerializer.Serialize(new { id = "1", method, @params }, ApiDispatcher.Json));
        var doc = JsonDocument.Parse(response).RootElement;
        Assert.True(doc.GetProperty("ok").GetBoolean(), response);
        return doc.GetProperty("result");
    }

    [Fact]
    public void Bootstrap_ListsPacksThemesAndCategories()
    {
        var boot = Call("app.bootstrap");
        Assert.True(boot.GetProperty("packs").GetArrayLength() > 0);
        Assert.True(boot.GetProperty("themes").GetArrayLength() >= 5);
        Assert.Empty(boot.GetProperty("themeErrors").EnumerateArray());
        Assert.Equal(5, boot.GetProperty("scopeNames").GetArrayLength());
    }

    [Fact]
    public void Generate_SaveAndExport_RoundTrip()
    {
        var concept = Call("concept.generate", new { recipe = new { seed = "418920", scope = 2, featureCreep = true } });
        Assert.False(string.IsNullOrEmpty(concept.GetProperty("title").GetString()));
        var entry = Call("vault.save", new { concept, notes = "n" });
        var id = entry.GetProperty("id").GetString();
        var export = Call("export.render", new { entryId = id, format = "markdown" });
        Assert.Contains(concept.GetProperty("title").GetString()!, export.GetProperty("content").GetString());
        Assert.Equal(1, Call("vault.list").GetArrayLength());
    }

    [Fact]
    public void RecipeCodes_ReproduceTheSameConcept()
    {
        var recipe = new { seed = "crab office", chaos = 0.8, scope = 3, featureCreep = true };
        var original = Call("concept.generate", new { recipe });
        var code = Call("recipe.encode", new { recipe = original.GetProperty("recipe") }).GetProperty("code").GetString();
        var decoded = Call("recipe.decode", new { code });
        var again = Call("concept.generate", new { recipe = decoded.GetProperty("recipe") });
        Assert.Equal(original.GetProperty("pitch").GetString(), again.GetProperty("pitch").GetString());
        Assert.Equal(original.GetProperty("creep").GetRawText(), again.GetProperty("creep").GetRawText());
    }

    [Fact]
    public void DisablingAPack_RemovesItsItems()
    {
        Call("packs.setEnabled", new { id = "core", enabled = false });
        var options = Call("category.options", new { category = "genre" });
        Assert.DoesNotContain(options.EnumerateArray(), o => o.GetProperty("packId").GetString() == "core");
        Call("packs.setEnabled", new { id = "core", enabled = true });
        Assert.Contains(Call("category.options", new { category = "genre" }).EnumerateArray(), o => o.GetProperty("packId").GetString() == "core");
    }

    [Fact]
    public void ImportPack_ValidatesAndRefusesBuiltInIds()
    {
        var bad = Call("packs.import", new { fileName = "x.json", content = "{ nope" });
        Assert.False(bad.GetProperty("ok").GetBoolean());
        var clash = Call("packs.import", new { fileName = "c.json", content = """{ "format": 1, "id": "core", "name": "Fake", "version": "1" }""" });
        Assert.False(clash.GetProperty("ok").GetBoolean());
        var good = Call("packs.import", new { fileName = "crabs.json", content = """{ "format": 1, "id": "crabs", "name": "Crabs", "version": "1", "items": { "mood": [ { "id": "pinchy", "label": "Pinchy", "forms": { "adj": "pinchy" } } ] } }""" });
        Assert.True(good.GetProperty("ok").GetBoolean());
        Assert.Contains(Call("packs.list").EnumerateArray(), p => p.GetProperty("id").GetString() == "crabs" && p.GetProperty("enabled").GetBoolean());
        Assert.True(Call("packs.delete", new { id = "crabs" }).GetProperty("deleted").GetBoolean());
    }

    [Fact]
    public void Errors_AreReportedNotThrown()
    {
        var response = JsonDocument.Parse(_api.HandleJson("""{ "id": 5, "method": "nope" }""")).RootElement;
        Assert.False(response.GetProperty("ok").GetBoolean());
        Assert.Equal(5, response.GetProperty("id").GetInt32());
        Assert.False(JsonDocument.Parse(_api.HandleJson("garbage")).RootElement.GetProperty("ok").GetBoolean());
        Assert.False(JsonDocument.Parse(_api.HandleJson("""{ "method": "vault.get", "params": { "id": "nope" } }""")).RootElement.GetProperty("ok").GetBoolean());
    }
}
