namespace BadGameGen.Core.Content;

/// <summary>Identifies exactly which content a concept was generated from.</summary>
public sealed record PackRef(string Id, string Version);

/// <summary>
/// The merged, read-only view over all enabled packs. Packs are always merged in id order,
/// so enabling packs in a different order can never change what a seed produces.
/// </summary>
public sealed class ContentLibrary
{
    private readonly Dictionary<string, List<ContentItem>> _items = new();
    private readonly Dictionary<string, List<TemplateDef>> _templates = new();
    private readonly Dictionary<string, ContentItem> _byFullId = new();

    public IReadOnlyList<ContentPack> Packs { get; }
    public IReadOnlyList<PackRef> Signature { get; }

    public ContentLibrary(IEnumerable<ContentPack> packs)
    {
        Packs = packs
            .GroupBy(p => p.Id).Select(g => g.First()) // duplicate pack ids: first wins (loader reports it)
            .OrderBy(p => p.Id, StringComparer.Ordinal)
            .ToList();
        Signature = Packs.Select(p => new PackRef(p.Id, p.Version)).ToList();

        foreach (var pack in Packs)
        {
            foreach (var (category, items) in pack.Items)
            {
                if (!_items.TryGetValue(category, out var list)) _items[category] = list = new();
                foreach (var item in items)
                {
                    list.Add(item);
                    _byFullId[item.FullId] = item;
                }
            }
            foreach (var (section, templates) in pack.Templates)
            {
                if (!_templates.TryGetValue(section, out var list)) _templates[section] = list = new();
                list.AddRange(templates);
            }
        }
    }

    public IReadOnlyList<ContentItem> Items(string category) =>
        _items.TryGetValue(category, out var list) ? list : [];

    public IReadOnlyList<TemplateDef> Templates(string section) =>
        _templates.TryGetValue(section, out var list) ? list : [];

    public bool TryGetItem(string fullId, out ContentItem item) => _byFullId.TryGetValue(fullId, out item!);

    public int ItemCount => _byFullId.Count;
}
