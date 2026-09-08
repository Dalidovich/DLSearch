using DLSearch.Output;
using DLSearch.Search;
using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Output;

public class ResultPrinterTests
{
    [Fact]
    public void PrintUsage_WritesMessage()
    {
        Assert.Contains("usage: f", Print(printer => printer.PrintUsage("usage: f <pattern>")));
    }

    [Fact]
    public void PrintError_WritesMessage()
    {
        Assert.Contains("Unknown flag '-x'.", Print(printer => printer.PrintError("Unknown flag '-x'.")));
    }

    [Fact]
    public void PrintNoResults_WritesNotice()
    {
        Assert.Contains("No matches.", Print(printer => printer.PrintNoResults()));
    }

    [Fact]
    public void PrintNames_WithoutMatches_WritesNothing()
    {
        Assert.Equal(string.Empty, Print(printer => printer.PrintNames([])));
    }

    [Fact]
    public void PrintNames_WritesHeaderWithCount()
    {
        string output = Print(printer => printer.PrintNames([Name("src", "wall.cs"), Name("docs", "firewall.md")]));

        Assert.Contains("NAMES  2", output);
    }

    [Fact]
    public void PrintNames_WritesDirectorySizeAndDate()
    {
        string output = Print(printer => printer.PrintNames([Name("src", "wall.cs", 2048, new DateTime(2020, 2, 14))]));

        Assert.Contains("src" + Path.DirectorySeparatorChar, output);
        Assert.Contains("wall.cs", output);
        Assert.Contains("2 KB", output);
        Assert.Contains("14.02.2020", output);
    }

    [Fact]
    public void PrintNames_RootDirectory_IsShownAsDot()
    {
        string output = Print(printer => printer.PrintNames([Name(string.Empty, "wallpaper")]));

        Assert.Contains("  .  wallpaper", output);
    }

    [Fact]
    public void PrintNames_SortsByDirectoryThenName()
    {
        string output = Print(printer => printer.PrintNames(
        [
            Name("src", "zebra.cs"),
            Name("src", "alpha.cs"),
            Name("docs", "notes.md")
        ]));

        int docs = output.IndexOf("notes.md", StringComparison.Ordinal);
        int alpha = output.IndexOf("alpha.cs", StringComparison.Ordinal);
        int zebra = output.IndexOf("zebra.cs", StringComparison.Ordinal);

        Assert.True(docs < alpha);
        Assert.True(alpha < zebra);
    }

    [Fact]
    public void PrintContent_WritesHeaderOnlyOnce()
    {
        string output = Print(printer =>
        {
            printer.PrintContent(Content("a.cs"));
            printer.PrintContent(Content("b.cs"));
        });

        Assert.Equal(1, CountOccurrences(output, "CONTENT"));
    }

    [Fact]
    public void PrintContent_WritesPathAndNumberedLines()
    {
        string output = Print(printer => printer.PrintContent(new ContentMatch(
            Path.Combine("src", "Wall.cs"),
            [new LineMatch(42, "var wall = 1;", [new CharRange(4, 4)])])));

        Assert.Contains(Path.Combine("src", "Wall.cs"), output);
        Assert.Contains("42", output);
        Assert.Contains("var wall = 1;", output);
    }

    [Fact]
    public void PrintContent_SanitizesControlCharacters()
    {
        string output = Print(printer => printer.PrintContent(new ContentMatch(
            "a.cs",
            [new LineMatch(1, "wall\tstone", [new CharRange(0, 4)])])));

        Assert.Contains("wall stone", output);
    }

    [Fact]
    public void PrintStatistics_WritesEveryCounter()
    {
        var statistics = new SearchStatistics();
        statistics.CountDirectory();
        statistics.CountFile();
        statistics.CountRead(1024);
        statistics.CountSkipped(SkipReason.Size);
        statistics.CountSkipped(SkipReason.Binary);
        statistics.CountSkipped(SkipReason.Denied);
        statistics.CountNameMatch();
        statistics.CountContentFile(2);

        string output = Print(printer => printer.PrintStatistics(statistics, TimeSpan.FromSeconds(14)));

        Assert.Contains("1 dirs", output);
        Assert.Contains("1 files", output);
        Assert.Contains("1 read", output);
        Assert.Contains("1 KB", output);
        Assert.Contains("3 skipped (1 size", output);
        Assert.Contains("1 binary", output);
        Assert.Contains("1 denied)", output);
        Assert.Contains("1 names", output);
        Assert.Contains("2 content in 1 file", output);
        Assert.Contains("0:14", output);
        Assert.DoesNotContain("interrupted", output);
    }

    [Fact]
    public void PrintStatistics_SeveralContentFiles_UsePluralForm()
    {
        var statistics = new SearchStatistics();
        statistics.CountContentFile(1);
        statistics.CountContentFile(1);

        string output = Print(printer => printer.PrintStatistics(statistics, TimeSpan.Zero));

        Assert.Contains("2 content in 2 files", output);
    }

    [Fact]
    public void PrintStatistics_Interrupted_IsMarked()
    {
        var statistics = new SearchStatistics { Interrupted = true };

        Assert.Contains("interrupted", Print(printer => printer.PrintStatistics(statistics, TimeSpan.Zero)));
    }

    private static string Print(Action<ResultPrinter> action)
    {
        using var capture = new ConsoleCapture();
        action(new ResultPrinter(new ConsoleSurface()));
        return capture.Text;
    }

    private static NameMatch Name(string directory, string name, long size = 1024, DateTime? modified = null)
    {
        return new NameMatch(directory, name, size, modified ?? new DateTime(2020, 2, 14), new CharRange(0, 1));
    }

    private static ContentMatch Content(string path)
    {
        return new ContentMatch(path, [new LineMatch(1, "wall", [new CharRange(0, 4)])]);
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = text.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
