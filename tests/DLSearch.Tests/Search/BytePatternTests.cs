using System.Text;
using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class BytePatternTests
{
    private const string CyrillicLower = "\u043F\u0440\u0438\u0432\u0435\u0442";
    private const string CyrillicUpper = "\u041F\u0420\u0418\u0412\u0415\u0422";

    [Fact]
    public void TryCreate_EmptyText_ReturnsNull()
    {
        Assert.Null(BytePattern.TryCreate(string.Empty, PatternEncoding.Utf8, false));
    }

    [Fact]
    public void TryCreate_TextOutsideCp1251_ReturnsNull()
    {
        Assert.Null(BytePattern.TryCreate("\u65E5", PatternEncoding.Cp1251, false));
    }

    [Fact]
    public void TryCreate_ExposesEncodedBytes()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, true);

        Assert.Equal(PatternEncoding.Utf8, pattern.Encoding);
        Assert.Equal(4, pattern.Length);
        Assert.True(pattern.Bytes.SequenceEqual("wall"u8));
    }

    [Fact]
    public void IndexOf_CaseSensitive_FindsExactBytes()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, true);

        Assert.Equal(4, pattern.IndexOf("the wall is high"u8, 0));
    }

    [Fact]
    public void IndexOf_CaseSensitive_IgnoresDifferentCase()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, true);

        Assert.Equal(-1, pattern.IndexOf("the WALL is high"u8, 0));
    }

    [Fact]
    public void IndexOf_CaseInsensitive_FindsDifferentCase()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, false);

        Assert.Equal(4, pattern.IndexOf("the WaLL is high"u8, 0));
    }

    [Fact]
    public void IndexOf_CaseInsensitive_FindsCyrillicInUtf8()
    {
        BytePattern pattern = Create(CyrillicLower, PatternEncoding.Utf8, false);

        Assert.Equal(0, pattern.IndexOf(Encoding.UTF8.GetBytes(CyrillicUpper), 0));
    }

    [Fact]
    public void IndexOf_CaseInsensitive_FindsCyrillicInCp1251()
    {
        BytePattern pattern = Create(CyrillicUpper, PatternEncoding.Cp1251, false);

        Assert.Equal(0, pattern.IndexOf(Cp1251.TryGetBytes(CyrillicLower)!, 0));
    }

    [Fact]
    public void IndexOf_StartOffset_SkipsEarlierMatch()
    {
        BytePattern pattern = Create("abc", PatternEncoding.Utf8, true);

        Assert.Equal(3, pattern.IndexOf("abcabc"u8, 1));
    }

    [Fact]
    public void IndexOf_NegativeStart_ReturnsNotFound()
    {
        BytePattern pattern = Create("abc", PatternEncoding.Utf8, true);

        Assert.Equal(-1, pattern.IndexOf("abc"u8, -1));
    }

    [Fact]
    public void IndexOf_PatternLongerThanRemainder_ReturnsNotFound()
    {
        BytePattern pattern = Create("abc", PatternEncoding.Utf8, true);

        Assert.Equal(-1, pattern.IndexOf("abcabc"u8, 4));
    }

    [Fact]
    public void IndexOf_NoMatch_ReturnsNotFound()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, false);

        Assert.Equal(-1, pattern.IndexOf("floor and ceiling"u8, 0));
    }

    [Fact]
    public void IndexOf_CaseInsensitive_MatchesTrailingOccurrence()
    {
        BytePattern pattern = Create("wall", PatternEncoding.Utf8, false);

        Assert.Equal(10, pattern.IndexOf("firewalls WALL"u8, 5));
    }

    private static BytePattern Create(string text, PatternEncoding encoding, bool caseSensitive)
    {
        BytePattern? pattern = BytePattern.TryCreate(text, encoding, caseSensitive);
        Assert.NotNull(pattern);
        return pattern!;
    }
}
