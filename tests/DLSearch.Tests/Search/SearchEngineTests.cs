using DLSearch.Cli;
using DLSearch.Output;
using DLSearch.Search;
using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Search;

public class SearchEngineTests
{
    [Fact]
    public async Task Run_ReportsNameBlockBeforeContentBlock()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wallpaper.txt", "nothing here\n");
        directory.WriteUtf8(Path.Combine("src", "render.cs"), "var wall = 1;\n");

        RunResult result = await RunAsync(directory, TestOptions.Create("wall"));

        int names = result.Output.IndexOf("NAMES", StringComparison.Ordinal);
        int content = result.Output.IndexOf("CONTENT", StringComparison.Ordinal);

        Assert.True(names >= 0);
        Assert.True(content > names);
        Assert.Contains("wallpaper.txt", result.Output);
        Assert.Contains("var wall = 1;", result.Output);
    }

    [Fact]
    public async Task Run_CollectsStatisticsForTheWholeTree()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.txt", "wall\n");
        directory.WriteUtf8(Path.Combine("src", "notes.txt"), "wall and wall\n");
        directory.WriteBytes(Path.Combine("src", "image.bin"), [.. "wall"u8.ToArray(), 0x00]);

        RunResult result = await RunAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(2, result.Statistics.Directories);
        Assert.Equal(3, result.Statistics.FilesSeen);
        Assert.Equal(3, result.Statistics.TotalFiles);
        Assert.Equal(3, result.Statistics.FilesProcessed);
        Assert.Equal(1, result.Statistics.NameMatches);
        Assert.Equal(2, result.Statistics.ContentMatches);
        Assert.Equal(2, result.Statistics.ContentFiles);
        Assert.Equal(1, result.Statistics.SkippedBinary);
    }

    [Fact]
    public async Task Run_WithoutMatches_PrintsNothingButProgress()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("floor.txt", "ceiling\n");

        RunResult result = await RunAsync(directory, TestOptions.Create("wall"));

        Assert.DoesNotContain("NAMES", result.Output);
        Assert.DoesNotContain("CONTENT", result.Output);
        Assert.Equal(0, result.Statistics.NameMatches);
        Assert.Equal(0, result.Statistics.ContentMatches);
    }

    [Fact]
    public async Task Run_JunkDirectory_IsExcludedFromBothBlocks()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8(Path.Combine("node_modules", "wall.txt"), "wall\n");

        RunResult result = await RunAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(0, result.Statistics.NameMatches);
        Assert.Equal(0, result.Statistics.ContentMatches);
        Assert.Equal(0, result.Statistics.FilesSeen);
    }

    [Fact]
    public async Task Run_ExtensionFilter_LimitsBothBlocks()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.cs", "wall\n");
        directory.WriteUtf8("wall.txt", "wall\n");

        RunResult result = await RunAsync(directory, TestOptions.Create("wall", extensions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs" }));

        Assert.Equal(1, result.Statistics.NameMatches);
        Assert.Equal(1, result.Statistics.ContentMatches);
    }

    [Fact]
    public async Task Run_MeasuresElapsedTime()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.txt", "wall\n");

        RunResult result = await RunAsync(directory, TestOptions.Create("wall"));

        Assert.True(result.Elapsed > TimeSpan.Zero);
    }

    [Fact]
    public async Task Run_CancelledSearch_CompletesWithoutThrowing()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.txt", "wall\n");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        using var capture = new ConsoleCapture();
        var surface = new ConsoleSurface();
        var statistics = new SearchStatistics();
        SearchOptions options = TestOptions.Create("wall");
        var engine = new SearchEngine(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics, surface, new ResultPrinter(surface));

        await engine.RunAsync(directory.Path, cancellation.Token);

        Assert.Equal(0, statistics.ContentMatches);
    }

    private static async Task<RunResult> RunAsync(TemporaryDirectory directory, SearchOptions options)
    {
        using var capture = new ConsoleCapture();
        var surface = new ConsoleSurface();
        var statistics = new SearchStatistics();
        var engine = new SearchEngine(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics, surface, new ResultPrinter(surface));

        await engine.RunAsync(directory.Path, CancellationToken.None);

        return new RunResult(capture.Text, statistics, engine.Elapsed);
    }

    private sealed record RunResult(string Output, SearchStatistics Statistics, TimeSpan Elapsed);
}
