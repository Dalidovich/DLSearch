using System.Globalization;
using System.Text;
using DLSearch.Search;

namespace DLSearch.Output;

public sealed class ResultPrinter
{
    private const char Separator = (char)0xB7;
    private const char LineGutter = (char)0x2502;
    private const int Indent = 2;
    private const int MinimumNumberWidth = 4;
    private const int MaximumDirectoryColumn = 40;
    private const int MaximumNameColumn = 48;

    private readonly ConsoleSurface _surface;
    private bool _contentHeaderPrinted;

    public ResultPrinter(ConsoleSurface surface)
    {
        _surface = surface;
    }

    public void PrintUsage(string message)
    {
        _surface.Write(message);
    }

    public void PrintError(string message)
    {
        _surface.Write(Decorate(Ansi.Error, message));
    }

    public void PrintNames(IReadOnlyList<NameMatch> matches)
    {
        if (matches.Count == 0)
        {
            return;
        }

        var ordered = matches
            .OrderBy(match => match.RelativeDirectory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int directoryColumn = Math.Min(MaximumDirectoryColumn, ordered.Max(match => DisplayDirectory(match.RelativeDirectory).Length));
        int nameColumn = Math.Min(MaximumNameColumn, ordered.Max(match => match.Name.Length));
        int sizeColumn = ordered.Max(match => Formatting.Size(match.Size).Length);

        var builder = new StringBuilder();
        builder.Append(Decorate(Ansi.Header, "NAMES")).Append("  ").Append(Formatting.Count(ordered.Count)).Append('\n');

        foreach (NameMatch match in ordered)
        {
            string directory = Clip(DisplayDirectory(match.RelativeDirectory), directoryColumn).PadRight(directoryColumn);
            (string name, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(match.Name, [match.Highlight], nameColumn);

            builder.Append(new string(' ', Indent));
            builder.Append(Decorate(Ansi.Directory, directory));
            builder.Append("  ");
            builder.Append(Decorate(Ansi.FileName, TextLayout.Highlight(name.PadRight(nameColumn), ranges, _surface.Decorated, Ansi.FileName)));
            builder.Append("  ");
            builder.Append(Decorate(Ansi.Muted, Formatting.Size(match.Size).PadLeft(sizeColumn)));
            builder.Append("   ");
            builder.Append(Decorate(Ansi.Muted, Formatting.Date(match.Modified)));
            builder.Append('\n');
        }

        _surface.Write(builder.ToString());
    }

    public void PrintContent(ContentMatch match)
    {
        var builder = new StringBuilder();

        if (!_contentHeaderPrinted)
        {
            builder.Append(Decorate(Ansi.Header, "CONTENT")).Append('\n');
            _contentHeaderPrinted = true;
        }

        int numberWidth = Math.Max(MinimumNumberWidth, match.Lines.Max(line => line.LineNumber).ToString(CultureInfo.InvariantCulture).Length);
        int textWidth = Math.Max(20, _surface.Width - Indent - 4 - numberWidth - 3);

        builder.Append(new string(' ', Indent));
        builder.Append(Decorate(Ansi.FileName, match.RelativePath));
        builder.Append('\n');

        foreach (LineMatch line in match.Lines)
        {
            (string text, IReadOnlyList<CharRange> ranges) = TextLayout.Fit(TextLayout.Sanitize(line.Text), line.Highlights, textWidth);

            builder.Append(new string(' ', Indent + 4));
            builder.Append(Decorate(Ansi.LineNumber, line.LineNumber.ToString(CultureInfo.InvariantCulture).PadLeft(numberWidth)));
            builder.Append(' ').Append(Decorate(Ansi.LineNumber, LineGutter.ToString())).Append(' ');
            builder.Append(TextLayout.Highlight(text, ranges, _surface.Decorated));
            builder.Append('\n');
        }

        _surface.Write(builder.ToString());
    }

    public void PrintNoResults()
    {
        _surface.Write(Decorate(Ansi.Muted, "No matches."));
    }

    public void PrintStatistics(SearchStatistics statistics, TimeSpan elapsed)
    {
        var builder = new StringBuilder();
        builder.Append(new string(' ', Indent));
        builder.Append(Formatting.Count(statistics.Directories)).Append(" dirs");
        Divide(builder).Append(Formatting.Count(statistics.FilesSeen)).Append(" files");
        Divide(builder).Append(Formatting.Count(statistics.FilesRead)).Append(" read");
        Divide(builder).Append(Formatting.Size(statistics.BytesRead));
        Divide(builder).Append(Formatting.Count(statistics.SkippedTotal)).Append(" skipped (");
        builder.Append(Formatting.Count(statistics.SkippedBySize)).Append(" size ").Append(Separator).Append(' ');
        builder.Append(Formatting.Count(statistics.SkippedBinary)).Append(" binary ").Append(Separator).Append(' ');
        builder.Append(Formatting.Count(statistics.SkippedDenied)).Append(" denied)");
        Divide(builder).Append(Formatting.Count(statistics.NameMatches)).Append(" names");
        Divide(builder).Append(Formatting.Count(statistics.ContentMatches)).Append(" content in ").Append(Formatting.Count(statistics.ContentFiles)).Append(statistics.ContentFiles == 1 ? " file" : " files");
        Divide(builder).Append(Formatting.Duration(elapsed));

        if (statistics.Interrupted)
        {
            Divide(builder).Append("interrupted");
        }

        _surface.Write(Decorate(Ansi.Muted, builder.ToString()));
    }

    private static StringBuilder Divide(StringBuilder builder)
    {
        return builder.Append(' ').Append(Separator).Append(' ');
    }

    private static string DisplayDirectory(string relativeDirectory)
    {
        return relativeDirectory.Length == 0 ? "." : relativeDirectory + Path.DirectorySeparatorChar;
    }

    private static string Clip(string text, int width)
    {
        return text.Length <= width ? text : string.Concat("...", text.AsSpan(text.Length - width + 3));
    }

    private string Decorate(string color, string text)
    {
        return _surface.Decorated ? color + text + Ansi.Reset : text;
    }
}
