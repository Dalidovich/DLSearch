using System.Text;

namespace DLSearch.Search;

public enum PatternEncoding
{
    Utf8,
    Cp1251
}

public static class PatternEncodingExtensions
{
    public static string Decode(this PatternEncoding encoding, ReadOnlySpan<byte> bytes)
    {
        return encoding == PatternEncoding.Utf8 ? Encoding.UTF8.GetString(bytes) : Cp1251.GetString(bytes);
    }

    public static int CountChars(this PatternEncoding encoding, ReadOnlySpan<byte> bytes)
    {
        return encoding == PatternEncoding.Utf8 ? Encoding.UTF8.GetCharCount(bytes) : bytes.Length;
    }

    public static int DecodeInto(this PatternEncoding encoding, ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        if (encoding == PatternEncoding.Utf8)
        {
            return Encoding.UTF8.GetChars(bytes, destination);
        }

        Cp1251.GetChars(bytes, destination);
        return bytes.Length;
    }

    public static int MaxCharCount(this PatternEncoding encoding, int byteCount)
    {
        return encoding == PatternEncoding.Utf8 ? Encoding.UTF8.GetMaxCharCount(byteCount) : byteCount;
    }
}
