namespace DLSearch.Search;

public sealed class PatternSet
{
    private PatternSet(string text, bool caseSensitive, IReadOnlyList<BytePattern> patterns)
    {
        Text = text;
        CaseSensitive = caseSensitive;
        Patterns = patterns;
        MaxPatternLength = patterns.Max(p => p.Length);
    }

    public string Text { get; }

    public bool CaseSensitive { get; }

    public StringComparison Comparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public IReadOnlyList<BytePattern> Patterns { get; }

    public int MaxPatternLength { get; }

    public static PatternSet Create(string text, bool caseSensitive)
    {
        var patterns = new List<BytePattern>(2);

        var utf8 = BytePattern.TryCreate(text, PatternEncoding.Utf8, caseSensitive);
        if (utf8 is not null)
        {
            patterns.Add(utf8);
        }

        var cp1251 = BytePattern.TryCreate(text, PatternEncoding.Cp1251, caseSensitive);
        if (cp1251 is not null && (utf8 is null || !cp1251.Bytes.SequenceEqual(utf8.Bytes)))
        {
            patterns.Add(cp1251);
        }

        return new PatternSet(text, caseSensitive, patterns);
    }
}
