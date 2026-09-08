using DLSearch.Output;
using DLSearch.Search;
using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Output;

public class ProgressIndicatorTests
{
    [Fact]
    public void Render_BeforeWalkFinishes_ShowsCountersWithoutPercentage()
    {
        var statistics = new SearchStatistics();
        statistics.CountDirectory();
        statistics.CountFile();
        statistics.CountFile();
        statistics.CountRead(2048);
        statistics.CountNameMatch();

        string rendered = AnsiText.Strip(new ProgressIndicator(statistics).Render(TimeSpan.FromSeconds(65)));

        Assert.Contains("1 dirs", rendered);
        Assert.Contains("2 files", rendered);
        Assert.Contains("2 KB", rendered);
        Assert.Contains("1 matches", rendered);
        Assert.Contains("1:05", rendered);
        Assert.DoesNotContain("%", rendered);
    }

    [Fact]
    public void Render_BeforeWalkFinishes_AdvancesSpinner()
    {
        var indicator = new ProgressIndicator(new SearchStatistics());

        string first = AnsiText.Strip(indicator.Render(TimeSpan.Zero));
        string second = AnsiText.Strip(indicator.Render(TimeSpan.Zero));

        Assert.NotEqual(first[0], second[0]);
    }

    [Fact]
    public void Render_AfterWalkFinishes_ShowsProgressBar()
    {
        var statistics = new SearchStatistics();
        for (int i = 0; i < 4; i++)
        {
            statistics.CountFile();
        }

        statistics.FreezeTotalFiles();
        statistics.CountProcessed();

        string rendered = AnsiText.Strip(new ProgressIndicator(statistics).Render(TimeSpan.Zero));

        Assert.Contains("25%", rendered);
        Assert.Contains("1/4", rendered);
        Assert.Contains(new string('\u2588', 5) + new string('\u2591', 15), rendered);
    }

    [Fact]
    public void Render_AfterWalkWithoutFiles_ShowsFullBar()
    {
        var statistics = new SearchStatistics();
        statistics.FreezeTotalFiles();

        string rendered = AnsiText.Strip(new ProgressIndicator(statistics).Render(TimeSpan.Zero));

        Assert.Contains("100%", rendered);
        Assert.Contains("0/0", rendered);
    }

    [Fact]
    public void Render_CountsNameAndContentMatchesTogether()
    {
        var statistics = new SearchStatistics();
        statistics.CountNameMatch();
        statistics.CountContentFile(4);
        statistics.FreezeTotalFiles();

        string rendered = AnsiText.Strip(new ProgressIndicator(statistics).Render(TimeSpan.Zero));

        Assert.Contains("5 matches", rendered);
    }
}
