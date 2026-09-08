using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class PatternSetTests
{
    private const string CyrillicWord = "\u043F\u0440\u0438\u0432\u0435\u0442";

    [Fact]
    public void Create_AsciiPattern_ProducesSingleRepresentation()
    {
        PatternSet set = PatternSet.Create("wall", false);

        Assert.Single(set.Patterns);
        Assert.Equal(PatternEncoding.Utf8, set.Patterns[0].Encoding);
        Assert.Equal(4, set.MaxPatternLength);
    }

    [Fact]
    public void Create_CyrillicPattern_ProducesBothRepresentations()
    {
        PatternSet set = PatternSet.Create(CyrillicWord, false);

        Assert.Equal(2, set.Patterns.Count);
        Assert.Equal(PatternEncoding.Utf8, set.Patterns[0].Encoding);
        Assert.Equal(PatternEncoding.Cp1251, set.Patterns[1].Encoding);
    }

    [Fact]
    public void Create_CyrillicPattern_UsesLongestRepresentationLength()
    {
        PatternSet set = PatternSet.Create(CyrillicWord, false);

        Assert.Equal(12, set.MaxPatternLength);
    }

    [Fact]
    public void Create_PatternOutsideCp1251_KeepsUtf8Only()
    {
        PatternSet set = PatternSet.Create("\u65E5\u672C", false);

        Assert.Single(set.Patterns);
        Assert.Equal(PatternEncoding.Utf8, set.Patterns[0].Encoding);
    }

    [Fact]
    public void Create_KeepsTextAndCaseSensitivity()
    {
        PatternSet set = PatternSet.Create("Wall", true);

        Assert.Equal("Wall", set.Text);
        Assert.True(set.CaseSensitive);
        Assert.Equal(StringComparison.Ordinal, set.Comparison);
    }

    [Fact]
    public void Comparison_CaseInsensitiveSet_IsOrdinalIgnoreCase()
    {
        Assert.Equal(StringComparison.OrdinalIgnoreCase, PatternSet.Create("wall", false).Comparison);
    }
}
