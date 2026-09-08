namespace DLSearch.Search;

public readonly record struct CharRange(int Start, int Length);

public readonly record struct FileCandidate(string FullPath, string RelativeDirectory, string Name, long Length);

public sealed record NameMatch(string RelativeDirectory, string Name, long Size, DateTime Modified, CharRange Highlight);

public sealed record LineMatch(long LineNumber, string Text, IReadOnlyList<CharRange> Highlights);

public sealed record ContentMatch(string RelativePath, IReadOnlyList<LineMatch> Lines);

public enum SkipReason
{
    None,
    Size,
    Binary,
    Denied
}
