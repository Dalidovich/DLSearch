namespace DLSearch.Cli;

public sealed class SearchOptions
{
    public required string Pattern { get; init; }

    public required IReadOnlyCollection<string>? Extensions { get; init; }

    public required long MaxFileSize { get; init; }

    public required bool IncludeJunkDirectories { get; init; }

    public required bool IncludeHidden { get; init; }

    public required bool ForceCaseSensitive { get; init; }

    public bool CaseSensitive => ForceCaseSensitive || Pattern.Any(char.IsUpper);

    public bool SizeLimitDisabled => MaxFileSize == long.MaxValue;
}
