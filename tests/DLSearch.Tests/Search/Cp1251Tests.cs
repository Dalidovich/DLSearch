using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class Cp1251Tests
{
    private const char ReplacementCharacter = '\uFFFD';
    private const byte UnmappedByte = 0x98;

    [Theory]
    [InlineData('\u0410', 0xC0)]
    [InlineData('\u042F', 0xDF)]
    [InlineData('\u0430', 0xE0)]
    [InlineData('\u044F', 0xFF)]
    [InlineData('\u0401', 0xA8)]
    [InlineData('\u0451', 0xB8)]
    [InlineData('\u2116', 0xB9)]
    [InlineData('\u00A0', 0xA0)]
    [InlineData('A', 0x41)]
    public void TryGetBytes_MapsSupportedCharacters(char character, byte expected)
    {
        byte[]? bytes = Cp1251.TryGetBytes(character.ToString());

        Assert.NotNull(bytes);
        Assert.Equal([expected], bytes!);
    }

    [Fact]
    public void TryGetBytes_AsciiText_MatchesAsciiBytes()
    {
        byte[]? bytes = Cp1251.TryGetBytes("wall");

        Assert.Equal("wall"u8.ToArray(), bytes!);
    }

    [Fact]
    public void TryGetBytes_EmptyText_ReturnsEmptyArray()
    {
        Assert.Empty(Cp1251.TryGetBytes(string.Empty)!);
    }

    [Theory]
    [InlineData("\u65E5")]
    [InlineData("wall\u2603")]
    public void TryGetBytes_UnsupportedCharacter_ReturnsNull(string text)
    {
        Assert.Null(Cp1251.TryGetBytes(text));
    }

    [Fact]
    public void GetString_DecodesCyrillicRange()
    {
        Assert.Equal("\u0410\u0430", Cp1251.GetString([0xC0, 0xE0]));
    }

    [Fact]
    public void GetString_UnmappedByte_BecomesReplacementCharacter()
    {
        Assert.Equal(ReplacementCharacter.ToString(), Cp1251.GetString([UnmappedByte]));
    }

    [Fact]
    public void GetString_EmptySource_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Cp1251.GetString([]));
    }

    [Fact]
    public void GetChars_FillsDestinationSpan()
    {
        Span<char> destination = stackalloc char[3];

        Cp1251.GetChars([0x41, 0xC0, 0xFF], destination);

        Assert.Equal("A\u0410\u044F", new string(destination));
    }

    [Fact]
    public void EveryMappedByte_SurvivesRoundTrip()
    {
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            if (value == UnmappedByte)
            {
                continue;
            }

            string decoded = Cp1251.GetString([(byte)value]);
            Assert.NotEqual(ReplacementCharacter, decoded[0]);
            Assert.Equal([(byte)value], Cp1251.TryGetBytes(decoded)!);
        }
    }
}
