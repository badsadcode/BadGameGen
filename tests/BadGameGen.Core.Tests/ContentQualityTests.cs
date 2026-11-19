using System.Text.RegularExpressions;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Tests;

/// <summary>Smoke-tests the real content: hundreds of concepts must all read cleanly.</summary>
public class ContentQualityTests
{
    private static readonly Regex Broken = new(@"\{|\}|\[a\]|  | [,.;:!?]|\b(a|an) (a|an)\b|\.\.(?!\.)|,,", RegexOptions.Compiled);

    [Fact]
    public void GeneratedText_HasNoTemplateDebris()
    {
        var gen = new Generator(TestContent.BuiltInLibrary());
        var problems = new List<string>();
        for (int i = 0; i < 400; i++)
        {
            var c = gen.Generate(new Recipe { Seed = $"q{i}", Chaos = (i % 5) / 4.0, Scope = i % 5, Silliness = (i % 3) / 2.0, FeatureCreep = true });
            var texts = new List<string> { c.Title, c.Hook, c.Pitch, c.BadIdea, c.MightWork };
            texts.AddRange(c.Expanded.Select(s => s.Body));
            texts.AddRange(c.Creep.Select(x => x.Pitch + " " + x.Connects));
            foreach (var t in texts.Where(t => Broken.IsMatch(t)))
                problems.Add($"q{i}: {t}");
            if (string.IsNullOrWhiteSpace(c.Hook) || string.IsNullOrWhiteSpace(c.Pitch) || string.IsNullOrWhiteSpace(c.Title))
                problems.Add($"q{i}: missing hook/pitch/title");
            if (c.Warnings.Count > 0) problems.Add($"q{i}: {string.Join("; ", c.Warnings)}");
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems.Take(25)));
    }
}
