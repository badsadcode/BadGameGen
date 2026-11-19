using BadGameGen.Core.Content;

namespace BadGameGen.Core.Tests;

public class PackLoaderTests
{
    private const string Header = "\"format\": 1, \"id\": \"test\", \"name\": \"Test\", \"version\": \"1.0.0\"";

    [Fact]
    public void BuiltInPacks_AreAllValid()
    {
        var catalog = PackCatalog.Load([(TestContent.BuiltInPacksDir, true)]);
        Assert.NotEmpty(catalog.Results);
        foreach (var r in catalog.Results)
            Assert.True(r.IsUsable, $"{Path.GetFileName(r.Source)}: {string.Join("; ", r.Errors)}");
    }

    [Fact]
    public void ValidPack_LoadsWithNamespacedIds()
    {
        var r = PackLoader.LoadJson($$"""
            { {{Header}}, // comments are fine
              "items": { "mood": [ { "id": "moody", "label": "Moody", "forms": { "adj": "moody" } }, ] } }
            """, "test.json");
        Assert.True(r.IsUsable, string.Join("; ", r.Errors));
        Assert.Equal("test:mood.moody", r.Pack!.Items["mood"][0].FullId);
    }

    [Theory]
    [InlineData("{ not json", "Invalid JSON")]
    [InlineData("{ \"format\": 9, \"id\": \"x\", \"name\": \"X\", \"version\": \"1\" }", "Unsupported format")]
    [InlineData("{ \"format\": 1, \"id\": \"Bad Id\", \"name\": \"X\", \"version\": \"1\" }", "Pack id")]
    public void BrokenPack_ReportsErrorsInsteadOfThrowing(string json, string expected)
    {
        var r = PackLoader.LoadJson(json, "broken.json");
        Assert.False(r.IsUsable);
        Assert.Contains(r.Errors, e => e.Contains(expected));
    }

    [Theory]
    [InlineData("\"wizards\": [ { \"id\": \"a\", \"label\": \"A\" } ]", "Unknown category")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": {} } ]", "missing required form 'adj'")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": { \"adj\": \"\" } } ]", "must not be empty")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": { \"adj\": \"{x}\" } } ]", "braces")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": { \"adj\": \"x\" } }, { \"id\": \"a\", \"label\": \"B\", \"forms\": { \"adj\": \"y\" } } ]", "duplicate id")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": { \"adj\": \"x\" }, \"weight\": 0 } ]", "weight")]
    [InlineData("\"mood\": [ { \"id\": \"a\", \"label\": \"A\", \"forms\": { \"adj\": \"x\" }, \"scope\": [3, 1] } ]", "scope")]
    [InlineData("\"creep\": [ { \"id\": \"a\", \"label\": \"A\", \"kind\": \"lol\", \"complexity\": 3, \"forms\": { \"pitch\": \"x\" } } ]", "kind")]
    [InlineData("\"creep\": [ { \"id\": \"a\", \"label\": \"A\", \"kind\": \"system\", \"complexity\": 3, \"forms\": { \"pitch\": \"{bogus.noun}\" } } ]", "unknown field")]
    public void InvalidItems_AreRejected(string items, string expected)
    {
        var r = PackLoader.LoadJson($"{{ {Header}, \"items\": {{ {items} }} }}", "t.json");
        Assert.False(r.IsUsable);
        Assert.Contains(r.Errors, e => e.Contains(expected));
    }

    [Theory]
    [InlineData("\"nope\": [ { \"id\": \"a\", \"text\": \"x\" } ]", "Unknown template section")]
    [InlineData("\"hook\": [ { \"id\": \"a\", \"text\": \"{first} game\" } ]", "not allowed")]
    [InlineData("\"title\": [ { \"id\": \"a\", \"text\": \"{title}\" } ]", "not allowed")]
    [InlineData("\"hook\": [ { \"id\": \"a\", \"text\": \"\" } ]", "text is missing")]
    public void InvalidTemplates_AreRejected(string templates, string expected)
    {
        var r = PackLoader.LoadJson($"{{ {Header}, \"templates\": {{ {templates} }} }}", "t.json");
        Assert.False(r.IsUsable);
        Assert.Contains(r.Errors, e => e.Contains(expected));
    }

    [Fact]
    public void DuplicatePackIds_AcrossFiles_AreReported()
    {
        var dir = Directory.CreateTempSubdirectory("bgg-packs");
        try
        {
            var json = $"{{ {Header} }}";
            File.WriteAllText(Path.Combine(dir.FullName, "a.json"), json);
            File.WriteAllText(Path.Combine(dir.FullName, "b.json"), json);
            File.WriteAllText(Path.Combine(dir.FullName, "c.json"), "garbage");
            var catalog = PackCatalog.Load([(dir.FullName, false)]);
            Assert.Equal(3, catalog.Results.Count);
            Assert.Single(catalog.UsablePacks);
            Assert.Contains(catalog.Results, r => r.Errors.Any(e => e.Contains("Duplicate pack id")));
        }
        finally { dir.Delete(true); }
    }
}
