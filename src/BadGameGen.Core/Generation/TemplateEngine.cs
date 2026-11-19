using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BadGameGen.Core.Content;

namespace BadGameGen.Core.Generation;

/// <summary>A parsed {field.form} reference inside a template.</summary>
public sealed record TemplateRef(string Field, bool IsList, string Form);

/// <summary>
/// Minimal, predictable text templating.
///
/// Syntax:
///   {setting.in}        a form of the picked item ("label" works for every item)
///   {setting}           same as {setting.label}
///   {secondary*.noun}   every secondary mechanic, joined as "a, b and c"
///   {title}             the generated title
///   [a] word            becomes "a word" or "an word" depending on pronunciation
///
/// A template is only eligible when every field it references exists in the concept,
/// so optional fields (multiplayer, progression...) never leave holes in sentences.
/// </summary>
public static partial class TemplateEngine
{
    [GeneratedRegex(@"\{([a-zA-Z]+)(\*)?(?:\.([a-zA-Z]+))?\}")]
    private static partial Regex RefRegex();

    [GeneratedRegex(@"\[a\]\s+([^\s]+)")]
    private static partial Regex ArticleRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpace();

    [GeneratedRegex(@"\s+([,.;:!?])")]
    private static partial Regex SpaceBeforePunct();

    [GeneratedRegex(@"(?<!\.)\.\.(?!\.)")]
    private static partial Regex DoubleDot();

    [GeneratedRegex(@",\s*,")]
    private static partial Regex DoubleComma();

    /// <summary>Template field name → content category it draws from.</summary>
    public static readonly IReadOnlyDictionary<string, string> FieldCategories = new Dictionary<string, string>
    {
        ["genre"] = Categories.Genre,
        ["subgenre"] = Categories.Subgenre,
        ["setting"] = Categories.Setting,
        ["era"] = Categories.Era,
        ["protagonist"] = Categories.Protagonist,
        ["perspective"] = Categories.Perspective,
        ["objective"] = Categories.Objective,
        ["loop"] = Categories.Loop,
        ["mechanic"] = Categories.Mechanic,
        ["secondary"] = Categories.Mechanic,
        ["progression"] = Categories.Progression,
        ["threat"] = Categories.Threat,
        ["resource"] = Categories.Resource,
        ["mood"] = Categories.Mood,
        ["visual"] = Categories.Visual,
        ["twist"] = Categories.Twist,
        ["multiplayer"] = Categories.Multiplayer,
        ["first"] = Categories.TitleFirst,
        ["last"] = Categories.TitleLast,
    };

    /// <summary>Fields that are plain strings rather than content items.</summary>
    public static readonly IReadOnlySet<string> SpecialFields = new HashSet<string> { "title" };

    public static IReadOnlyList<TemplateRef> Parse(string text) =>
        RefRegex().Matches(text)
            .Select(m => new TemplateRef(m.Groups[1].Value, m.Groups[2].Success, m.Groups[3].Success ? m.Groups[3].Value : "label"))
            .ToList();

    /// <summary>Returns validation errors for a template text (empty when valid).</summary>
    public static List<string> Validate(string text, IReadOnlySet<string>? allowedFields = null)
    {
        var errors = new List<string>();
        foreach (var r in Parse(text))
        {
            if (allowedFields != null && !allowedFields.Contains(r.Field))
            {
                errors.Add($"field '{{{r.Field}}}' is not allowed here");
                continue;
            }
            if (SpecialFields.Contains(r.Field))
            {
                if (r.Form != "label" || r.IsList) errors.Add($"'{{{r.Field}}}' takes no form or list marker");
                continue;
            }
            if (!FieldCategories.TryGetValue(r.Field, out var cat))
            {
                errors.Add($"unknown field '{r.Field}'");
                continue;
            }
            var info = Categories.Get(cat);
            if (r.Form != "label" && !info.RequiredForms.Contains(r.Form))
                errors.Add($"'{r.Field}' has no guaranteed form '{r.Form}' (valid: label, {string.Join(", ", info.RequiredForms)})");
            if (r.IsList && r.Field != "secondary")
                errors.Add($"list marker '*' is only supported on 'secondary'");
        }
        // Braces that are not valid references are almost always typos.
        var stripped = RefRegex().Replace(text, "");
        if (stripped.Contains('{') || stripped.Contains('}'))
            errors.Add("contains stray '{' or '}'");
        return errors;
    }

    public static bool CanRender(string text, TemplateContext ctx) =>
        Parse(text).All(r => ctx.Has(r.Field));

    public static string Render(string text, TemplateContext ctx)
    {
        var raw = RefRegex().Replace(text, m =>
        {
            var field = m.Groups[1].Value;
            var isList = m.Groups[2].Success;
            var form = m.Groups[3].Success ? m.Groups[3].Value : "label";
            return ctx.Resolve(field, form, isList);
        });
        return Polish(raw);
    }

    /// <summary>Fixes articles, spacing and sentence capitalisation.</summary>
    public static string Polish(string text)
    {
        var s = ArticleRegex().Replace(text, m => $"{Article(m.Groups[1].Value)} {m.Groups[1].Value}");
        s = MultiSpace().Replace(s, " ");
        s = SpaceBeforePunct().Replace(s, "$1");
        s = DoubleComma().Replace(s, ",");
        s = DoubleDot().Replace(s, ".");
        s = s.Trim();
        return CapitaliseSentences(s);
    }

    private static readonly string[] AnExceptions = ["hour", "honest", "honor", "honour", "heir", "herb"];
    private static readonly string[] AExceptions = ["uni", "use", "usu", "ure", "uro", "eu", "ewe", "one", "once", "ufo", "uter", "ukulele"];
    private const string VowelSoundLetters = "AEFHILMNORSX";

    /// <summary>"a" or "an" for the following word, with common English exceptions.</summary>
    public static string Article(string nextWord)
    {
        var word = nextWord.TrimStart('"', '\'', '(', '“', '‘');
        if (word.Length == 0) return "a";
        var letters = new string(word.TakeWhile(char.IsLetterOrDigit).ToArray());
        if (letters.Length == 0) return "a";

        // Acronyms such as RPG, FPS, MMO, UFO: decided by how the first letter is spoken.
        if (letters.Length >= 2 && letters.All(c => char.IsUpper(c) || char.IsDigit(c)))
        {
            if (letters.StartsWith("UFO", StringComparison.Ordinal)) return "a";
            if (char.IsDigit(letters[0])) return letters[0] == '8' || letters.StartsWith("11") || letters.StartsWith("18") ? "an" : "a";
            return VowelSoundLetters.Contains(letters[0]) ? "an" : "a";
        }
        if (char.IsDigit(letters[0])) return letters[0] == '8' || letters.StartsWith("11") || letters.StartsWith("18") ? "an" : "a";

        var lower = letters.ToLowerInvariant();
        if (AnExceptions.Any(lower.StartsWith)) return "an";
        if (AExceptions.Any(lower.StartsWith)) return "a";
        return "aeiou".Contains(lower[0]) ? "an" : "a";
    }

    private static string CapitaliseSentences(string s)
    {
        var sb = new StringBuilder(s);
        bool capNext = true;
        for (int i = 0; i < sb.Length; i++)
        {
            char c = sb[i];
            if (capNext && char.IsLetter(c))
            {
                sb[i] = char.ToUpper(c, CultureInfo.InvariantCulture);
                capNext = false;
            }
            else if (capNext && char.IsDigit(c))
            {
                capNext = false;
            }
            else if (c is '.' or '!' or '?')
            {
                // Only start a new sentence when followed by whitespace (avoids "3.5" and "e.g.").
                capNext = i + 1 < sb.Length && char.IsWhiteSpace(sb[i + 1]);
            }
        }
        return sb.ToString();
    }

    private static readonly HashSet<string> SmallWords =
        ["a", "an", "the", "of", "and", "or", "in", "on", "at", "to", "for", "with", "by", "from", "vs", "vs."];

    /// <summary>Title Case For Game Names, Leaving Small Words Alone.</summary>
    public static string TitleCase(string s)
    {
        var words = Polish(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            bool afterColon = i > 0 && words[i - 1].EndsWith(':');
            if (i > 0 && !afterColon && SmallWords.Contains(w.ToLowerInvariant()))
            {
                words[i] = w.ToLowerInvariant();
                continue;
            }
            // Capitalise first letter of each hyphenated part, keep the rest as written (keeps "UFO", "McRat").
            words[i] = string.Join('-', w.Split('-').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
        }
        return string.Join(' ', words);
    }

    /// <summary>"a", "a and b", "a, b and c".</summary>
    public static string JoinList(IReadOnlyList<string> parts) => parts.Count switch
    {
        0 => "",
        1 => parts[0],
        2 => $"{parts[0]} and {parts[1]}",
        _ => string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1],
    };
}

/// <summary>Values available to templates while rendering one concept.</summary>
public sealed class TemplateContext
{
    private readonly Dictionary<string, List<ContentItem>> _fields = new();
    public string? Title { get; set; }

    public void Set(string field, ContentItem item) => _fields[field] = [item];

    public void Add(string field, ContentItem item)
    {
        if (!_fields.TryGetValue(field, out var list)) _fields[field] = list = new();
        list.Add(item);
    }

    public IReadOnlyList<ContentItem> GetAll(string field) =>
        _fields.TryGetValue(field, out var list) ? list : [];

    public bool Has(string field) =>
        field == "title" ? Title != null : _fields.TryGetValue(field, out var l) && l.Count > 0;

    public string Resolve(string field, string form, bool isList)
    {
        if (field == "title") return Title ?? "";
        if (!_fields.TryGetValue(field, out var items) || items.Count == 0) return "";
        return isList
            ? TemplateEngine.JoinList(items.Select(i => i.Form(form)).ToList())
            : items[0].Form(form);
    }
}
