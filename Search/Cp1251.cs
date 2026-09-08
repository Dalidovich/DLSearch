namespace DLSearch.Search;

public static class Cp1251
{
    private static readonly ushort[] HighRange =
    [
        0x0402, 0x0403, 0x201A, 0x0453, 0x201E, 0x2026, 0x2020, 0x2021,
        0x20AC, 0x2030, 0x0409, 0x2039, 0x040A, 0x040C, 0x040B, 0x040F,
        0x0452, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
        0x0000, 0x2122, 0x0459, 0x203A, 0x045A, 0x045C, 0x045B, 0x045F,
        0x00A0, 0x040E, 0x045E, 0x0408, 0x00A4, 0x0490, 0x00A6, 0x00A7,
        0x0401, 0x00A9, 0x0404, 0x00AB, 0x00AC, 0x00AD, 0x00AE, 0x0407,
        0x00B0, 0x00B1, 0x0406, 0x0456, 0x0491, 0x00B5, 0x00B6, 0x00B7,
        0x0451, 0x2116, 0x0454, 0x00BB, 0x0458, 0x0405, 0x0455, 0x0457
    ];

    private const char Replacement = (char)0xFFFD;

    private static readonly char[] ByteToChar = BuildDecodeTable();
    private static readonly Dictionary<char, byte> CharToByte = BuildEncodeTable();

    public static byte[]? TryGetBytes(string text)
    {
        var result = new byte[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            if (!CharToByte.TryGetValue(text[i], out byte value))
            {
                return null;
            }

            result[i] = value;
        }

        return result;
    }

    public static void GetChars(ReadOnlySpan<byte> source, Span<char> destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = ByteToChar[source[i]];
        }
    }

    public static string GetString(ReadOnlySpan<byte> source)
    {
        char[] buffer = new char[source.Length];
        GetChars(source, buffer);
        return new string(buffer);
    }

    private static char[] BuildDecodeTable()
    {
        var table = new char[256];

        for (int i = 0; i < 0x80; i++)
        {
            table[i] = (char)i;
        }

        for (int i = 0; i < HighRange.Length; i++)
        {
            ushort code = HighRange[i];
            table[0x80 + i] = code == 0 ? Replacement : (char)code;
        }

        for (int i = 0xC0; i <= 0xFF; i++)
        {
            table[i] = (char)(0x0410 + (i - 0xC0));
        }

        return table;
    }

    private static Dictionary<char, byte> BuildEncodeTable()
    {
        var table = new Dictionary<char, byte>(256);

        for (int i = 0; i < ByteToChar.Length; i++)
        {
            char character = ByteToChar[i];
            if (character != Replacement)
            {
                table[character] = (byte)i;
            }
        }

        return table;
    }
}
