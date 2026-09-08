using System.Buffers;

namespace DLSearch.Search;

public sealed class BytePattern
{
    private readonly byte[] _bytes;
    private readonly string _text;
    private readonly bool _ignoreCase;
    private readonly SearchValues<byte>? _leadingBytes;

    private BytePattern(byte[] bytes, string text, PatternEncoding encoding, bool ignoreCase, SearchValues<byte>? leadingBytes)
    {
        _bytes = bytes;
        _text = text;
        _ignoreCase = ignoreCase;
        _leadingBytes = leadingBytes;
        Encoding = encoding;
    }

    public PatternEncoding Encoding { get; }

    public int Length => _bytes.Length;

    public ReadOnlySpan<byte> Bytes => _bytes;

    public static BytePattern? TryCreate(string text, PatternEncoding encoding, bool caseSensitive)
    {
        byte[]? bytes = Encode(text, encoding);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        if (caseSensitive)
        {
            return new BytePattern(bytes, text, encoding, false, null);
        }

        var leading = new HashSet<byte> { bytes[0] };

        foreach (string variant in new[] { text.ToUpperInvariant(), text.ToLowerInvariant() })
        {
            byte[]? variantBytes = Encode(variant, encoding);
            if (variantBytes is { Length: > 0 } && variantBytes.Length == bytes.Length)
            {
                leading.Add(variantBytes[0]);
            }
        }

        return new BytePattern(bytes, text, encoding, true, SearchValues.Create([.. leading]));
    }

    public int IndexOf(ReadOnlySpan<byte> buffer, int start)
    {
        if (start < 0 || start + _bytes.Length > buffer.Length)
        {
            return -1;
        }

        if (!_ignoreCase)
        {
            int found = buffer[start..].IndexOf(_bytes);
            return found < 0 ? -1 : start + found;
        }

        int position = start;

        while (position + _bytes.Length <= buffer.Length)
        {
            int candidate = buffer[position..].IndexOfAny(_leadingBytes!);
            if (candidate < 0)
            {
                return -1;
            }

            position += candidate;
            if (position + _bytes.Length > buffer.Length)
            {
                return -1;
            }

            if (EqualsAt(buffer.Slice(position, _bytes.Length)))
            {
                return position;
            }

            position++;
        }

        return -1;
    }

    private bool EqualsAt(ReadOnlySpan<byte> window)
    {
        if (window.SequenceEqual(_bytes))
        {
            return true;
        }

        int maxChars = Encoding.MaxCharCount(window.Length);
        Span<char> decoded = maxChars <= 256 ? stackalloc char[256] : new char[maxChars];
        int count = Encoding.DecodeInto(window, decoded);
        return MemoryExtensions.Equals(decoded[..count], _text.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static byte[]? Encode(string text, PatternEncoding encoding)
    {
        return encoding == PatternEncoding.Utf8 ? System.Text.Encoding.UTF8.GetBytes(text) : Cp1251.TryGetBytes(text);
    }
}
