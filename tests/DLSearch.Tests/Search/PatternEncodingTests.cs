using System.Text;
using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class PatternEncodingTests
{
    private const string CyrillicWord = "\u0442\u0435\u0441\u0442";

    [Fact]
    public void Decode_Utf8_ReturnsOriginalText()
    {
        Assert.Equal(CyrillicWord, PatternEncoding.Utf8.Decode(Encoding.UTF8.GetBytes(CyrillicWord)));
    }

    [Fact]
    public void Decode_Cp1251_ReturnsOriginalText()
    {
        Assert.Equal(CyrillicWord, PatternEncoding.Cp1251.Decode(Cp1251.TryGetBytes(CyrillicWord)!));
    }

    [Fact]
    public void CountChars_Utf8_CountsDecodedCharacters()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CyrillicWord);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(4, PatternEncoding.Utf8.CountChars(bytes));
    }

    [Fact]
    public void CountChars_Cp1251_EqualsByteCount()
    {
        Assert.Equal(4, PatternEncoding.Cp1251.CountChars(Cp1251.TryGetBytes(CyrillicWord)!));
    }

    [Fact]
    public void DecodeInto_Utf8_WritesCharactersAndReturnsCount()
    {
        Span<char> destination = stackalloc char[16];

        int written = PatternEncoding.Utf8.DecodeInto(Encoding.UTF8.GetBytes(CyrillicWord), destination);

        Assert.Equal(4, written);
        Assert.Equal(CyrillicWord, new string(destination[..written]));
    }

    [Fact]
    public void DecodeInto_Cp1251_WritesCharactersAndReturnsCount()
    {
        Span<char> destination = stackalloc char[16];

        int written = PatternEncoding.Cp1251.DecodeInto(Cp1251.TryGetBytes(CyrillicWord)!, destination);

        Assert.Equal(4, written);
        Assert.Equal(CyrillicWord, new string(destination[..written]));
    }

    [Fact]
    public void MaxCharCount_Cp1251_EqualsByteCount()
    {
        Assert.Equal(10, PatternEncoding.Cp1251.MaxCharCount(10));
    }

    [Fact]
    public void MaxCharCount_Utf8_MatchesFrameworkEstimate()
    {
        Assert.Equal(Encoding.UTF8.GetMaxCharCount(10), PatternEncoding.Utf8.MaxCharCount(10));
    }
}
