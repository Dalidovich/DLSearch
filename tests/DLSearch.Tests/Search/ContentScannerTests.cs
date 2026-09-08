using System.Text;
using DLSearch.Cli;
using DLSearch.Search;
using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Search;

public class ContentScannerTests
{
    private const string CyrillicWord = "\u043F\u0440\u0438\u0432\u0435\u0442";

    [Fact]
    public void Scan_Utf8File_ReportsLineNumberTextAndHighlight()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "one\ntwo wall\nthree\n");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal(2, line.LineNumber);
        Assert.Equal("two wall", line.Text);
        Assert.Equal(new CharRange(4, 4), Assert.Single(line.Highlights));
        Assert.Equal(1, statistics.ContentFiles);
        Assert.Equal(1, statistics.ContentMatches);
    }

    [Fact]
    public void Scan_MatchInFirstLine_ReportsLineOne()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        Assert.Equal(1, Assert.Single(match!.Lines).LineNumber);
    }

    [Fact]
    public void Scan_UsesRelativeDirectoryInReportedPath()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        Assert.Equal(Path.Combine("sub", "notes.txt"), match!.RelativePath);
    }

    [Fact]
    public void Scan_WindowsLineEndings_TrimCarriageReturn()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "one\r\nwall stone\r\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal(2, line.LineNumber);
        Assert.Equal("wall stone", line.Text);
    }

    [Fact]
    public void Scan_SeveralOccurrencesOnOneLine_ProduceOneLineWithManyHighlights()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall and wall\n");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal([new CharRange(0, 4), new CharRange(9, 4)], line.Highlights);
        Assert.Equal(1, statistics.ContentMatches);
    }

    [Fact]
    public void Scan_SeveralLines_AreReportedInOrder()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall\nfloor\nwall\n");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        Assert.Equal([1, 3], match!.Lines.Select(line => line.LineNumber));
        Assert.Equal(2, statistics.ContentMatches);
    }

    [Fact]
    public void Scan_SmartCase_MatchesDifferentCase()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "The WALL\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        Assert.NotNull(match);
    }

    [Fact]
    public void Scan_ForcedCaseSensitivity_IgnoresDifferentCase()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "The WALL\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall", forceCaseSensitive: true));

        Assert.Null(match);
    }

    [Fact]
    public void Scan_Utf8Cyrillic_ReportsCharacterHighlightNotByteOffset()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "\u0442\u0435\u0441\u0442 " + CyrillicWord + "\n");

        (ContentMatch? match, _) = Scan(path, TestOptions.Create(CyrillicWord));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal("\u0442\u0435\u0441\u0442 " + CyrillicWord, line.Text);
        Assert.Equal(new CharRange(5, 6), Assert.Single(line.Highlights));
    }

    [Fact]
    public void Scan_Cp1251File_FindsCyrillicWithoutEncodingDetection()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteBytes("notes.txt", Cp1251.TryGetBytes("\u0442\u0435\u0441\u0442 " + CyrillicWord + "\n")!);

        (ContentMatch? match, _) = Scan(path, TestOptions.Create(CyrillicWord));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal("\u0442\u0435\u0441\u0442 " + CyrillicWord, line.Text);
        Assert.Equal(new CharRange(5, 6), Assert.Single(line.Highlights));
    }

    [Fact]
    public void Scan_Utf16LittleEndianFile_IsSearchedAsText()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteEncoded("notes.txt", "alpha\nwallpaper\n", new UnicodeEncoding(false, true));

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal(2, line.LineNumber);
        Assert.Equal("wallpaper", line.Text);
        Assert.Equal(new CharRange(0, 4), Assert.Single(line.Highlights));
    }

    [Fact]
    public void Scan_Utf16BigEndianFile_IsSearchedAsText()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteEncoded("notes.txt", "alpha\nwallpaper\n", new UnicodeEncoding(true, true));

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wall"));

        Assert.Equal("wallpaper", Assert.Single(match!.Lines).Text);
    }

    [Fact]
    public void Scan_Utf16File_SupportsCyrillic()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteEncoded("notes.txt", CyrillicWord + "\n", new UnicodeEncoding(false, true));

        (ContentMatch? match, _) = Scan(path, TestOptions.Create(CyrillicWord));

        Assert.Equal(CyrillicWord, Assert.Single(match!.Lines).Text);
    }

    [Fact]
    public void Scan_BinaryFile_IsSkipped()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteBytes("image.bin", [.. "wall"u8.ToArray(), 0x00, .. "wall"u8.ToArray()]);

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        Assert.Null(match);
        Assert.Equal(1, statistics.SkippedBinary);
        Assert.Equal(0, statistics.FilesRead);
    }

    [Fact]
    public void Scan_FileAboveSizeLimit_IsSkippedWithoutReading()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", new string('a', 100) + "wall");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall", maxFileSize: 10));

        Assert.Null(match);
        Assert.Equal(1, statistics.SkippedBySize);
        Assert.Equal(0, statistics.FilesRead);
    }

    [Fact]
    public void Scan_DisabledSizeLimit_ReadsOversizedFile()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", new string('a', 100) + "wall");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall", maxFileSize: long.MaxValue));

        Assert.NotNull(match);
        Assert.Equal(0, statistics.SkippedBySize);
    }

    [Fact]
    public void Scan_EmptyFile_IsCountedAsReadWithoutMatches()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteBytes("empty.txt", []);

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        Assert.Null(match);
        Assert.Equal(1, statistics.FilesRead);
        Assert.Equal(0, statistics.BytesRead);
    }

    [Fact]
    public void Scan_FileWithoutMatches_IsFullyRead()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "floor and ceiling\n");

        (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

        Assert.Null(match);
        Assert.Equal(1, statistics.FilesRead);
        Assert.Equal(new FileInfo(path).Length, statistics.BytesRead);
        Assert.Equal(0, statistics.ContentFiles);
    }

    [Fact]
    public void Scan_LockedFile_IsCountedAsDenied()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall\n");

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            (ContentMatch? match, SearchStatistics statistics) = Scan(path, TestOptions.Create("wall"));

            Assert.Null(match);
            Assert.Equal(1, statistics.SkippedDenied);
        }
    }

    [Fact]
    public void Scan_MatchSpanningBufferBoundary_IsFound()
    {
        const int fillerLines = 2621;
        var content = new StringBuilder();

        for (int i = 0; i < fillerLines; i++)
        {
            content.Append('a', 99).Append('\n');
        }

        content.Append('a', 40).Append("wallpaper").Append('\n');

        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("big.txt", content.ToString());

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("wallpaper"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal(fillerLines + 1, line.LineNumber);
        Assert.EndsWith("wallpaper", line.Text);
        Assert.Equal(new CharRange(line.Text.Length - 9, 9), Assert.Single(line.Highlights));
    }

    [Fact]
    public void Scan_VeryLongLine_IsTruncatedAroundTheMatch()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("long.txt", new string('a', 5000) + "needle" + new string('a', 5000));

        (ContentMatch? match, _) = Scan(path, TestOptions.Create("needle"));

        LineMatch line = Assert.Single(match!.Lines);
        Assert.Equal(8192, line.Text.Length);
        Assert.Equal(new CharRange(4096, 6), Assert.Single(line.Highlights));
    }

    [Fact]
    public void Scan_Cancelled_ThrowsOperationCanceled()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.WriteUtf8("notes.txt", "wall\n");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var statistics = new SearchStatistics();
        SearchOptions options = TestOptions.Create("wall");
        var scanner = new ContentScanner(PatternSet.Create(options.Pattern, options.CaseSensitive), options, statistics);

        Assert.Throws<OperationCanceledException>(() => scanner.Scan(Candidate(path), cancellation.Token));
    }

    private static (ContentMatch? Match, SearchStatistics Statistics) Scan(string path, SearchOptions options)
    {
        var statistics = new SearchStatistics();
        var scanner = new ContentScanner(PatternSet.Create(options.Pattern, options.CaseSensitive), options, statistics);

        return (scanner.Scan(Candidate(path), CancellationToken.None), statistics);
    }

    private static FileCandidate Candidate(string path)
    {
        var file = new FileInfo(path);
        return new FileCandidate(path, "sub", file.Name, file.Length);
    }
}
