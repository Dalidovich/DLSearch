using DLSearch.Output;
using DLSearch.Search;

namespace DLSearch.Tests.Output;

public class TextLayoutTests
{
    private const char Ellipsis = '\u2026';

    [Fact]
    public void Sanitize_ReplacesControlCharactersWithSpaces()
    {
        string result = TextLayout.Sanitize("a\tb\0cd");

        Assert.Equal("a b cd", result);
    }

    [Fact]
    public void Sanitize_KeepsPrintableCharactersAndLength()
    {
        const string text = "wall and floor";

        Assert.Equal(text, TextLayout.Sanitize(text));
    }

    [Fact]
    public void Sanitize_LongText_UsesHeapBuffer()
    {
        string text = new string('a', 600) + "\t";

        string result = TextLayout.Sanitize(text);

        Assert.Equal(601, result.Length);
        Assert.EndsWith("a ", result);
    }

    [Fact]
    public void Fit_TextShorterThanWidth_IsUnchanged()
    {
        var ranges = new List<CharRange> { new(0, 4) };

        (string text, IReadOnlyList<CharRange> result) = TextLayout.Fit("wall", ranges, 40);

        Assert.Equal("wall", text);
        Assert.Same(ranges, result);
    }

    [Fact]
    public void Fit_NonPositiveWidth_IsUnchanged()
    {
        (string text, _) = TextLayout.Fit("wall", [new CharRange(0, 4)], 0);

        Assert.Equal("wall", text);
    }

    [Fact]
    public void Fit_MatchNearStart_CutsOnTheRight()
    {
        string source = "wall" + new string('a', 96);

        (string text, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(source, [new CharRange(0, 4)], 20);

        Assert.Equal(20, text.Length);
        Assert.StartsWith("wall", text);
        Assert.Equal(Ellipsis, text[^1]);
        Assert.Equal(new CharRange(0, 4), Assert.Single(ranges));
    }

    [Fact]
    public void Fit_MatchNearEnd_CutsOnTheLeft()
    {
        string source = new string('a', 90) + "wall" + new string('a', 6);

        (string text, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(source, [new CharRange(90, 4)], 20);

        Assert.Equal(20, text.Length);
        Assert.Equal(Ellipsis, text[0]);
        Assert.NotEqual(Ellipsis, text[^1]);
        Assert.Equal(new CharRange(10, 4), Assert.Single(ranges));
    }

    [Fact]
    public void Fit_MatchInTheMiddle_CutsOnBothSides()
    {
        string source = new string('a', 50) + "wall" + new string('a', 50);

        (string text, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(source, [new CharRange(50, 4)], 30);

        Assert.Equal(30, text.Length);
        Assert.Equal(Ellipsis, text[0]);
        Assert.Equal(Ellipsis, text[^1]);
        CharRange moved = Assert.Single(ranges);
        Assert.Equal("wall", text.Substring(moved.Start, moved.Length));
    }

    [Fact]
    public void Fit_RangesOutsideWindow_AreDropped()
    {
        string source = "wall" + new string('a', 200) + "wall";

        (_, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(source, [new CharRange(0, 4), new CharRange(204, 4)], 20);

        Assert.Single(ranges);
    }

    [Fact]
    public void Highlight_WithoutDecoration_ReturnsPlainText()
    {
        Assert.Equal("wall", TextLayout.Highlight("wall", [new CharRange(0, 4)], false));
    }

    [Fact]
    public void Highlight_WithoutRanges_ReturnsPlainText()
    {
        Assert.Equal("wall", TextLayout.Highlight("wall", [], true));
    }

    [Fact]
    public void Highlight_WrapsRangeInAnsiCodes()
    {
        string result = TextLayout.Highlight("the wall", [new CharRange(4, 4)], true);

        Assert.Equal("the " + Ansi.Highlight + "wall" + Ansi.Reset, result);
    }

    [Fact]
    public void Highlight_RestoresBaseColorAfterEachRange()
    {
        string result = TextLayout.Highlight("wall.", [new CharRange(0, 4)], true, Ansi.FileName);

        Assert.Equal(Ansi.Highlight + "wall" + Ansi.Reset + Ansi.FileName + ".", result);
    }

    [Fact]
    public void Highlight_SeveralRanges_AreAllDecorated()
    {
        string result = TextLayout.Highlight("wall wall", [new CharRange(0, 4), new CharRange(5, 4)], true);

        Assert.Equal(Ansi.Highlight + "wall" + Ansi.Reset + " " + Ansi.Highlight + "wall" + Ansi.Reset, result);
    }

    [Fact]
    public void Highlight_RangeBeyondText_IsIgnored()
    {
        string result = TextLayout.Highlight("wall", [new CharRange(2, 40)], true);

        Assert.Equal("wall", result);
    }

    [Fact]
    public void Highlight_OverlappingRange_IsIgnored()
    {
        string result = TextLayout.Highlight("wallpaper", [new CharRange(0, 4), new CharRange(2, 4)], true);

        Assert.Equal(Ansi.Highlight + "wall" + Ansi.Reset + "paper", result);
    }
}
