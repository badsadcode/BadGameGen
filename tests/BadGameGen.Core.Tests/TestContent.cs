using System.Text.Json;
using BadGameGen.Core.Content;

namespace BadGameGen.Core.Tests;

/// <summary>Small synthetic packs with predictable behaviour, plus access to the real built-in packs.</summary>
internal static class TestContent
{
    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "content", "packs"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    public static string BuiltInPacksDir => Path.Combine(RepoRoot, "content", "packs");

    public static ContentLibrary BuiltInLibrary() => PackCatalog.Load([(BuiltInPacksDir, true)]).BuildLibrary();

    private static readonly Dictionary<string, Dictionary<string, string>> FormsByCategory = new()
    {
        ["genre"] = new() { ["adj"] = "test", ["noun"] = "test game" },
        ["subgenre"] = new() { ["noun"] = "puzzle game" },
        ["setting"] = new() { ["noun"] = "test town", ["in"] = "in a test town" },
        ["era"] = new() { ["in"] = "in the test era" },
        ["protagonist"] = new() { ["noun"] = "tester" },
        ["perspective"] = new() { ["noun"] = "top-down view" },
        ["objective"] = new() { ["verb"] = "test everything" },
        ["loop"] = new() { ["verb"] = "test and retest" },
        ["mechanic"] = new() { ["noun"] = "testing", ["verb"] = "test things" },
        ["progression"] = new() { ["verb"] = "unlock tests" },
        ["threat"] = new() { ["noun"] = "bugs", ["clause"] = "bugs multiply" },
        ["resource"] = new() { ["noun"] = "coffee" },
        ["mood"] = new() { ["adj"] = "tense" },
        ["visual"] = new() { ["noun"] = "pixel art" },
        ["twist"] = new() { ["clause"] = "the tests test you" },
        ["multiplayer"] = new() { ["phrase"] = "play solo" },
        ["titleFirst"] = new(),
        ["titleLast"] = new(),
    };

    /// <summary>A pack with <paramref name="perCategory"/> tag-free items per category.</summary>
    public static ContentPack Synthetic(string id = "synth", int perCategory = 8, string version = "1.0.0")
    {
        var pack = new ContentPack { Format = 1, Id = id, Name = id, Version = version };
        foreach (var (cat, forms) in FormsByCategory)
        {
            pack.Items[cat] = Enumerable.Range(0, perCategory).Select(i => new ContentItem
            {
                Id = $"{cat.ToLowerInvariant()}-{i}",
                Label = $"{id} {cat} {i}",
                Forms = forms.ToDictionary(kv => kv.Key, kv => $"{kv.Value} {i}"),
            }).ToList();
        }
        pack.Items["creep"] = Enumerable.Range(0, 10).Select(i => new ContentItem
        {
            Id = $"creep-{i}",
            Label = $"Creep {i}",
            Kind = Categories.CreepKinds[i % 4],
            Complexity = 1 + i % 5,
            Forms = new() { ["pitch"] = $"Extra thing {i} for the {{protagonist.noun}}." },
        }).ToList();
        pack.Templates["title"] = [new TemplateDef { Id = "t", Text = "{first} {last}" }];
        pack.Templates["hook"] = [new TemplateDef { Id = "h", Text = "[a] {genre.adj} {subgenre.noun} {setting.in}." }];
        pack.Templates["pitchOpen"] = [new TemplateDef { Id = "p", Text = "You play [a] {protagonist.noun} who must {objective.verb}." }];
        var errors = new List<string>();
        PackLoader.Validate(pack, errors, new List<string>());
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        return pack;
    }

    public static string Serialize(object o) => JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = false });
}
