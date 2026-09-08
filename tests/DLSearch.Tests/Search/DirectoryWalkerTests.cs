using System.Threading.Channels;
using DLSearch.Cli;
using DLSearch.Search;
using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Search;

public class DirectoryWalkerTests
{
    [Fact]
    public async Task Walk_FileNameContainingPattern_IsReported()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wallpaper.txt", string.Empty);
        directory.WriteUtf8("floor.txt", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        NameMatch match = Assert.Single(result.Names);
        Assert.Equal("wallpaper.txt", match.Name);
        Assert.Equal(string.Empty, match.RelativeDirectory);
        Assert.Equal(new CharRange(0, 4), match.Highlight);
        Assert.Equal(1, result.Statistics.NameMatches);
    }

    [Fact]
    public async Task Walk_NestedFile_ReportsRelativeDirectory()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8(Path.Combine("src", "render", "wall.cs"), string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(Path.Combine("src", "render"), Assert.Single(result.Names).RelativeDirectory);
    }

    [Fact]
    public async Task Walk_MatchInsideName_ReportsHighlightPosition()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("firewall-notes.md", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(new CharRange(4, 4), Assert.Single(result.Names).Highlight);
    }

    [Fact]
    public async Task Walk_SmartCase_MatchesNamesRegardlessOfCase()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("WallRenderer.cs", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Single(result.Names);
    }

    [Fact]
    public async Task Walk_ForcedCaseSensitivity_SkipsDifferentCase()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("WallRenderer.cs", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall", forceCaseSensitive: true));

        Assert.Empty(result.Names);
    }

    [Fact]
    public async Task Walk_DirectoryNameMatch_IsNotReported()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateDirectory("wallpapers");

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Empty(result.Names);
    }

    [Fact]
    public async Task Walk_EveryVisitedFile_BecomesScanCandidate()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("floor.txt", "text");
        directory.WriteUtf8(Path.Combine("src", "ceiling.txt"), "text");

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(2, result.Statistics.FilesSeen);
        Assert.Contains(result.Candidates, candidate => candidate.Name == "ceiling.txt" && candidate.RelativeDirectory == "src");
        Assert.All(result.Candidates, candidate => Assert.Equal(4, candidate.Length));
    }

    [Fact]
    public async Task Walk_JunkDirectories_AreSkippedByDefault()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8(Path.Combine(".git", "wall.txt"), string.Empty);
        directory.WriteUtf8(Path.Combine("node_modules", "wall.txt"), string.Empty);
        directory.WriteUtf8(Path.Combine("BIN", "wall.txt"), string.Empty);
        directory.WriteUtf8("wall.txt", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Equal("wall.txt", Assert.Single(result.Names).Name);
        Assert.Empty(Assert.Single(result.Names).RelativeDirectory);
    }

    [Fact]
    public async Task Walk_JunkDirectories_AreVisitedWhenRequested()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8(Path.Combine(".git", "wall.txt"), string.Empty);
        directory.WriteUtf8("wall.txt", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall", includeJunkDirectories: true));

        Assert.Equal(2, result.Names.Count);
    }

    [Fact]
    public async Task Walk_HiddenFile_IsSkippedByDefault()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("wall.txt", string.Empty);
        File.SetAttributes(path, FileAttributes.Hidden);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Empty(result.Names);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task Walk_HiddenFile_IsVisitedWhenRequested()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("wall.txt", string.Empty);
        File.SetAttributes(path, FileAttributes.Hidden);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall", includeHidden: true));

        Assert.Single(result.Names);
    }

    [Fact]
    public async Task Walk_HiddenDirectory_IsSkippedByDefault()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8(Path.Combine("secret", "wall.txt"), string.Empty);
        File.SetAttributes(directory.Combine("secret"), FileAttributes.Directory | FileAttributes.Hidden);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Empty(result.Names);
    }

    [Fact]
    public async Task Walk_ExtensionFilter_AppliesToNamesAndCandidates()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.cs", string.Empty);
        directory.WriteUtf8("wall.txt", string.Empty);
        directory.WriteUtf8("wall", string.Empty);

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall", extensions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".CS" }));

        Assert.Equal("wall.cs", Assert.Single(result.Names).Name);
        Assert.Equal("wall.cs", Assert.Single(result.Candidates).Name);
    }

    [Fact]
    public async Task Walk_CountsDirectoriesIncludingRoot()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateDirectory("one");
        directory.CreateDirectory(Path.Combine("one", "two"));

        WalkResult result = await WalkAsync(directory, TestOptions.Create("wall"));

        Assert.Equal(3, result.Statistics.Directories);
    }

    [Fact]
    public async Task Walk_MissingRoot_ReportsDeniedAndReturnsNoMatches()
    {
        using var directory = new TemporaryDirectory();
        string missing = directory.Combine("absent");

        var channel = Channel.CreateUnbounded<FileCandidate>();
        var statistics = new SearchStatistics();
        SearchOptions options = TestOptions.Create("wall");
        var walker = new DirectoryWalker(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics);

        List<NameMatch> names = await walker.WalkAsync(missing, channel.Writer, CancellationToken.None);

        Assert.Empty(names);
        Assert.Equal(1, statistics.SkippedDenied);
    }

    [Fact]
    public async Task Walk_CancelledBeforeStart_ReturnsNoMatches()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteUtf8("wall.txt", string.Empty);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var channel = Channel.CreateUnbounded<FileCandidate>();
        var statistics = new SearchStatistics();
        SearchOptions options = TestOptions.Create("wall");
        var walker = new DirectoryWalker(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics);

        List<NameMatch> names = await walker.WalkAsync(directory.Path, channel.Writer, cancellation.Token);

        Assert.Empty(names);
    }

    private static async Task<WalkResult> WalkAsync(TemporaryDirectory directory, SearchOptions options)
    {
        var channel = Channel.CreateUnbounded<FileCandidate>();
        var statistics = new SearchStatistics();
        var walker = new DirectoryWalker(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics);

        List<NameMatch> names = await walker.WalkAsync(directory.Path, channel.Writer, CancellationToken.None);
        channel.Writer.Complete();

        var candidates = new List<FileCandidate>();
        await foreach (FileCandidate candidate in channel.Reader.ReadAllAsync())
        {
            candidates.Add(candidate);
        }

        return new WalkResult(names, candidates, statistics);
    }

    private sealed record WalkResult(List<NameMatch> Names, List<FileCandidate> Candidates, SearchStatistics Statistics);
}
