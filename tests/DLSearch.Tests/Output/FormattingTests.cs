using DLSearch.Output;

namespace DLSearch.Tests.Output;

public class FormattingTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(7, "7")]
    [InlineData(999, "999")]
    [InlineData(1000, "1 000")]
    [InlineData(23104, "23 104")]
    [InlineData(1234567, "1 234 567")]
    public void Count_GroupsThousandsWithSpaces(long value, string expected)
    {
        Assert.Equal(expected, Formatting.Count(value));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    [InlineData(1125899906842624, "1024 TB")]
    public void Size_UsesLargestFittingUnit(long bytes, string expected)
    {
        Assert.Equal(expected, Formatting.Size(bytes));
    }

    [Fact]
    public void Duration_BelowOneHour_UsesMinutesAndSeconds()
    {
        Assert.Equal("0:00", Formatting.Duration(TimeSpan.Zero));
        Assert.Equal("0:07", Formatting.Duration(TimeSpan.FromSeconds(7)));
        Assert.Equal("1:05", Formatting.Duration(TimeSpan.FromSeconds(65)));
        Assert.Equal("59:59", Formatting.Duration(TimeSpan.FromSeconds(3599)));
    }

    [Fact]
    public void Duration_OneHourOrMore_IncludesHours()
    {
        Assert.Equal("1:00:00", Formatting.Duration(TimeSpan.FromHours(1)));
        Assert.Equal("2:03:04", Formatting.Duration(new TimeSpan(2, 3, 4)));
        Assert.Equal("25:00:00", Formatting.Duration(TimeSpan.FromHours(25)));
    }

    [Fact]
    public void Date_UnknownValue_IsRenderedAsDash()
    {
        Assert.Equal("-", Formatting.Date(DateTime.MinValue));
    }

    [Fact]
    public void Date_Today_ShowsTimeOnly()
    {
        Assert.Equal("13:05", Formatting.Date(DateTime.Today.AddHours(13).AddMinutes(5)));
    }

    [Fact]
    public void Date_Yesterday_ShowsWord()
    {
        Assert.Equal("yesterday", Formatting.Date(DateTime.Today.AddDays(-1).AddHours(9)));
    }

    [Fact]
    public void Date_OlderValue_ShowsFullDate()
    {
        Assert.Equal("14.02.2020", Formatting.Date(new DateTime(2020, 2, 14, 10, 30, 0)));
    }
}
