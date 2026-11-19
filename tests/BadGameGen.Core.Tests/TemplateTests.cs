using BadGameGen.Core.Content;
using BadGameGen.Core.Generation;

namespace BadGameGen.Core.Tests;

public class TemplateTests
{
    [Theory]
    [InlineData("apple", "an")]
    [InlineData("banana", "a")]
    [InlineData("hour", "an")]
    [InlineData("honest", "an")]
    [InlineData("unicorn", "a")]
    [InlineData("useless", "a")]
    [InlineData("umbrella", "an")]
    [InlineData("one-armed", "a")]
    [InlineData("European", "a")]
    [InlineData("RPG", "an")]
    [InlineData("FPS", "an")]
    [InlineData("MMO", "an")]
    [InlineData("UFO", "a")]
    [InlineData("VR", "a")]
    [InlineData("8-bit", "an")]
    [InlineData("1980s", "a")]
    public void Article_HandlesPronunciation(string word, string expected) =>
        Assert.Equal(expected, TemplateEngine.Article(word));

    [Fact]
    public void Polish_FixesSpacingArticlesAndCapitals()
    {
        Assert.Equal("An odd game in a town. It works.", TemplateEngine.Polish("[a] odd game  in a town . it works."));
        Assert.Equal("Just one more system...", TemplateEngine.Polish("just one more system..."));
        Assert.Equal("Fine, really.", TemplateEngine.Polish("fine, , really.."));
    }

    [Fact]
    public void Render_UsesFormsListsAndSkipsEmptyForms()
    {
        var ctx = new TemplateContext();
        ctx.Set("setting", Item("setting", "Town", ("noun", "town"), ("in", "in a town")));
        ctx.Set("era", Item("era", "Now", ("in", "")));
        ctx.Add("secondary", Item("mechanic", "Crafting", ("noun", "crafting"), ("verb", "craft")));
        ctx.Add("secondary", Item("mechanic", "Stealth", ("noun", "stealth"), ("verb", "sneak")));
        ctx.Add("secondary", Item("mechanic", "Trading", ("noun", "trading"), ("verb", "trade")));
        Assert.Equal("Set in a town. Juggle crafting, stealth and trading.",
            TemplateEngine.Render("set {setting.in} {era.in}. juggle {secondary*.noun}.", ctx));
    }

    [Fact]
    public void CanRender_RequiresEveryReferencedField()
    {
        var ctx = new TemplateContext();
        ctx.Set("mood", Item("mood", "Moody", ("adj", "moody")));
        Assert.True(TemplateEngine.CanRender("It's {mood.adj}.", ctx));
        Assert.False(TemplateEngine.CanRender("{multiplayer.phrase}.", ctx));
    }

    [Theory]
    [InlineData("{setting.in}", 0)]
    [InlineData("{nonsense.noun}", 1)]
    [InlineData("{setting.verb}", 1)]
    [InlineData("{mood*.adj}", 1)]
    [InlineData("{title.noun}", 1)]
    [InlineData("oops {setting.in", 1)]
    public void Validate_CatchesBadReferences(string text, int errorCount) =>
        Assert.Equal(errorCount, TemplateEngine.Validate(text).Count);

    [Theory]
    [InlineData("frost signal", "Frost Signal")]
    [InlineData("the tide of the frost", "The Tide of the Frost")]
    [InlineData("signal: the end", "Signal: The End")]
    [InlineData("UFO office", "UFO Office")]
    public void TitleCase_LeavesSmallWordsAlone(string input, string expected) =>
        Assert.Equal(expected, TemplateEngine.TitleCase(input));

    private static ContentItem Item(string category, string label, params (string Key, string Value)[] forms) =>
        new() { Id = label.ToLowerInvariant(), Label = label, Category = category, Forms = forms.ToDictionary(f => f.Key, f => f.Value) };
}
