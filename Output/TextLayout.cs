using System.Text;
using DLSearch.Search;

namespace DLSearch.Output;

public static class TextLayout
{
    private const char Ellipsis = (char)0x2026;

    public static string Sanitize(string text)
    {
        Span<char> buffer = text.Length <= 512 ? stackalloc char[text.Length] : new char[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            buffer[i] = char.IsControl(character) ? ' ' : character;
        }

        return new string(buffer);
    }

    public static (string Text, IReadOnlyList<CharRange> Ranges) Fit(string text, IReadOnlyList<CharRange> ranges, int width)
    {
        if (width <= 0 || text.Length <= width)
        {
            return (text, ranges);
        }

        int anchor = ranges.Count > 0 ? ranges[0].Start : 0;
        int start = Math.Max(0, anchor - width / 3);

        if (start + width > text.Length)
        {
            start = Math.Max(0, text.Length - width);
        }

        int length = Math.Min(width, text.Length - start);
        char[] slice = text.ToCharArray(start, length);

        bool cutLeft = start > 0;
        bool cutRight = start + length < text.Length;

        if (cutLeft)
        {
            slice[0] = Ellipsis;
        }

        if (cutRight)
        {
            slice[^1] = Ellipsis;
        }

        int lowerBound = cutLeft ? 1 : 0;
        int upperBound = cutRight ? slice.Length - 1 : slice.Length;

        var moved = new List<CharRange>();

        foreach (CharRange range in ranges)
        {
            int rangeStart = Math.Max(range.Start - start, lowerBound);
            int rangeEnd = Math.Min(range.Start - start + range.Length, upperBound);

            if (rangeEnd > rangeStart)
            {
                moved.Add(new CharRange(rangeStart, rangeEnd - rangeStart));
            }
        }

        return (new string(slice), moved);
    }

    public static string Highlight(string text, IReadOnlyList<CharRange> ranges, bool decorated, string? baseColor = null)
    {
        if (!decorated || ranges.Count == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + ranges.Count * 12);
        int position = 0;

        foreach (CharRange range in ranges)
        {
            if (range.Start < position || range.Start + range.Length > text.Length)
            {
                continue;
            }

            builder.Append(text, position, range.Start - position);
            builder.Append(Ansi.Highlight);
            builder.Append(text, range.Start, range.Length);
            builder.Append(Ansi.Reset);

            if (baseColor is not null)
            {
                builder.Append(baseColor);
            }

            position = range.Start + range.Length;
        }

        builder.Append(text, position, text.Length - position);
        return builder.ToString();
    }
}
