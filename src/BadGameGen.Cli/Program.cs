using System.Globalization;
using BadGameGen.Core.Content;
using BadGameGen.Core.Export;
using BadGameGen.Core.Generation;

// bgg: command-line companion for BadGameGen.
//   bgg validate [path...]          validate pack files or folders (default: built-in packs)
//   bgg generate [options]          print concepts
//   bgg stats                       library sizes per category

var argv = args.ToList();
var command = argv.Count > 0 ? argv[0] : "help";
string? Opt(string name)
{
    int i = argv.IndexOf("--" + name);
    return i >= 0 && i + 1 < argv.Count ? argv[i + 1] : null;
}
bool Flag(string name) => argv.Contains("--" + name);
double D(string name, double fallback) => double.TryParse(Opt(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

// --packs accepts several folders separated by commas.
var packDirs = (Opt("packs") ?? FindContentDir()).Split(',', StringSplitOptions.RemoveEmptyEntries);
string builtInDir = packDirs[0];
var packSources = packDirs.Select(d => (d, true)).ToList();

switch (command)
{
    case "validate":
    {
        var targets = argv.Skip(1).Where(a => !a.StartsWith("--")).ToList();
        if (targets.Count == 0) targets.AddRange(packDirs);
        int bad = 0;
        foreach (var target in targets)
        {
            var files = Directory.Exists(target) ? Directory.EnumerateFiles(target, "*.json").Order().ToList() : [target];
            foreach (var file in files)
            {
                var r = PackLoader.LoadFile(file, false);
                var counts = r.Pack == null ? "" : string.Join(", ", r.Pack.Items.Select(kv => $"{kv.Key} {kv.Value.Count}"));
                Console.WriteLine($"{(r.IsUsable ? "OK  " : "FAIL")} {Path.GetFileName(file)} {(r.Pack != null ? $"[{r.Pack.Id}@{r.Pack.Version}]" : "")} {counts}");
                foreach (var e in r.Errors) Console.WriteLine($"   error: {e}");
                foreach (var w in r.Warnings) Console.WriteLine($"   warn:  {w}");
                if (!r.IsUsable) bad++;
            }
        }
        return bad == 0 ? 0 : 1;
    }
    case "generate":
    {
        var catalog = PackCatalog.Load(packSources);
        foreach (var r in catalog.Results.Where(r => !r.IsUsable))
            Console.Error.WriteLine($"Skipping broken pack {Path.GetFileName(r.Source)}: {string.Join("; ", r.Errors.Take(3))}");
        var only = Opt("only")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var generator = new Generator(catalog.BuildLibrary(only));
        int count = int.TryParse(Opt("count"), out var c) ? c : 1;
        var format = Opt("format") ?? "text";
        var baseSeed = Opt("seed");
        for (int i = 0; i < count; i++)
        {
            var recipe = new Recipe
            {
                Seed = baseSeed == null ? Random.Shared.Next(100000, 999999).ToString(CultureInfo.InvariantCulture) : count == 1 ? baseSeed : $"{baseSeed}-{i}",
                Chaos = D("chaos", 0.35),
                Scope = (int)D("scope", 1),
                Silliness = D("silly", 0.5),
                FeatureCreep = Flag("creep"),
            };
            var concept = generator.Generate(recipe);
            if (format == "hook")
            {
                Console.WriteLine($"{concept.Title}: {concept.Hook}");
                continue;
            }
            Console.WriteLine(ConceptExporter.Export(concept, format switch
            {
                "md" or "markdown" => ExportFormat.Markdown,
                "json" => ExportFormat.Json,
                _ => ExportFormat.Text,
            }));
            foreach (var w in concept.Warnings) Console.WriteLine($"  ! {w}");
            Console.WriteLine();
        }
        return 0;
    }
    case "stats":
    {
        var catalog = PackCatalog.Load(packSources);
        var lib = catalog.BuildLibrary();
        foreach (var cat in Categories.All)
            Console.WriteLine($"{cat.Key,-14} {lib.Items(cat.Key).Count,5}");
        foreach (var s in TemplateSections.All)
            Console.WriteLine($"tpl:{s,-14} {lib.Templates(s).Count,3}");
        Console.WriteLine($"total items    {lib.ItemCount,5}");
        return 0;
    }
    default:
        Console.WriteLine("""
            bgg: BadGameGen command line. Terrible ideas, now in your terminal.

              bgg validate [file|folder ...]       check content packs
              bgg generate [options]               generate concepts
                  --seed <text> --count <n> --chaos <0..1> --scope <0..4> --silly <0..1>
                  --creep --format text|md|json|hook --only core,horror --packs <folder>
              bgg stats                            library sizes
            """);
        return 0;
}

static string FindContentDir()
{
    // Next to the binary (published), else walk up to the repository's content/packs.
    var local = Path.Combine(AppContext.BaseDirectory, "content", "packs");
    if (Directory.Exists(local)) return local;
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "content", "packs");
        if (Directory.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return local;
}
