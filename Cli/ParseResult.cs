namespace DLSearch.Cli;

public enum ParseStatus
{
    Success,
    Usage,
    Error
}

public sealed record ParseResult(ParseStatus Status, SearchOptions? Options, string? Message)
{
    public static ParseResult Ok(SearchOptions options) => new(ParseStatus.Success, options, null);

    public static ParseResult Usage() => new(ParseStatus.Usage, null, UsageText.Line);

    public static ParseResult Error(string message) => new(ParseStatus.Error, null, message);
}
