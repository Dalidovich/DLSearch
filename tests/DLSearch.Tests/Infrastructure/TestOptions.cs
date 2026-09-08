using DLSearch.Cli;

namespace DLSearch.Tests.Infrastructure;

public static class TestOptions
{
    public const long DefaultMaxFileSize = 32L * 1024 * 1024;

    public static SearchOptions Create(
        string pattern,
        long maxFileSize = DefaultMaxFileSize,
        IReadOnlyCollection<string>? extensions = null,
        bool includeJunkDirectories = false,
        bool includeHidden = false,
        bool forceCaseSensitive = false)
    {
        return new SearchOptions
        {
            Pattern = pattern,
            Extensions = extensions,
            MaxFileSize = maxFileSize,
            IncludeJunkDirectories = includeJunkDirectories,
            IncludeHidden = includeHidden,
            ForceCaseSensitive = forceCaseSensitive
        };
    }
}
