using DLSearch.Search;

namespace DLSearch.Tests.Search;

public class SearchStatisticsTests
{
    [Fact]
    public void NewInstance_HasEmptyCountersAndUnknownTotal()
    {
        var statistics = new SearchStatistics();

        Assert.Equal(0, statistics.Directories);
        Assert.Equal(0, statistics.FilesSeen);
        Assert.Equal(0, statistics.FilesRead);
        Assert.Equal(0, statistics.BytesRead);
        Assert.Equal(0, statistics.SkippedTotal);
        Assert.Equal(-1, statistics.TotalFiles);
        Assert.False(statistics.Interrupted);
    }

    [Fact]
    public void CountRead_AccumulatesFilesAndBytes()
    {
        var statistics = new SearchStatistics();

        statistics.CountRead(100);
        statistics.CountRead(20);

        Assert.Equal(2, statistics.FilesRead);
        Assert.Equal(120, statistics.BytesRead);
    }

    [Fact]
    public void CountSkipped_SeparatesReasons()
    {
        var statistics = new SearchStatistics();

        statistics.CountSkipped(SkipReason.Size);
        statistics.CountSkipped(SkipReason.Binary);
        statistics.CountSkipped(SkipReason.Binary);
        statistics.CountSkipped(SkipReason.Denied);
        statistics.CountSkipped(SkipReason.None);

        Assert.Equal(1, statistics.SkippedBySize);
        Assert.Equal(2, statistics.SkippedBinary);
        Assert.Equal(1, statistics.SkippedDenied);
        Assert.Equal(4, statistics.SkippedTotal);
    }

    [Fact]
    public void CountDenied_AddsToDeniedBucket()
    {
        var statistics = new SearchStatistics();

        statistics.CountDenied();

        Assert.Equal(1, statistics.SkippedDenied);
        Assert.Equal(1, statistics.SkippedTotal);
    }

    [Fact]
    public void CountContentFile_TracksFilesAndMatches()
    {
        var statistics = new SearchStatistics();

        statistics.CountContentFile(3);
        statistics.CountContentFile(2);

        Assert.Equal(2, statistics.ContentFiles);
        Assert.Equal(5, statistics.ContentMatches);
    }

    [Fact]
    public void FreezeTotalFiles_CapturesFilesSeenAtThatMoment()
    {
        var statistics = new SearchStatistics();
        statistics.CountFile();
        statistics.CountFile();

        statistics.FreezeTotalFiles();
        statistics.CountFile();

        Assert.Equal(2, statistics.TotalFiles);
        Assert.Equal(3, statistics.FilesSeen);
    }

    [Fact]
    public void Counters_AreSafeUnderConcurrentUpdates()
    {
        var statistics = new SearchStatistics();

        Parallel.For(0, 1000, _ =>
        {
            statistics.CountDirectory();
            statistics.CountFile();
            statistics.CountNameMatch();
            statistics.CountProcessed();
            statistics.CountRead(2);
        });

        Assert.Equal(1000, statistics.Directories);
        Assert.Equal(1000, statistics.FilesSeen);
        Assert.Equal(1000, statistics.NameMatches);
        Assert.Equal(1000, statistics.FilesProcessed);
        Assert.Equal(1000, statistics.FilesRead);
        Assert.Equal(2000, statistics.BytesRead);
    }
}
