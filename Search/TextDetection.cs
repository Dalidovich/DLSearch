using System.Text;

namespace DLSearch.Search;

public enum TextKind
{
    Text,
    Utf16LittleEndian,
    Utf16BigEndian,
    Binary
}

public static class TextDetection
{
    private const int InspectedBytes = 8192;

    public static TextKind Detect(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
        {
            return TextKind.Text;
        }

        if (head.Length >= 2 && head[0] == 0xFF && head[1] == 0xFE)
        {
            return TextKind.Utf16LittleEndian;
        }

        if (head.Length >= 2 && head[0] == 0xFE && head[1] == 0xFF)
        {
            return TextKind.Utf16BigEndian;
        }

        int inspected = Math.Min(head.Length, InspectedBytes);
        return head[..inspected].IndexOf((byte)0) >= 0 ? TextKind.Binary : TextKind.Text;
    }

    public static Encoding EncodingFor(TextKind kind)
    {
        return kind == TextKind.Utf16BigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
    }
}
