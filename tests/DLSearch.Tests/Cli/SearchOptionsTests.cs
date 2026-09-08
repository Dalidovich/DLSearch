using DLSearch.Tests.Infrastructure;

namespace DLSearch.Tests.Cli;

public class SearchOptionsTests
{
    [Theory]
    [InlineData("wall", false)]
    [InlineData("wall paper", false)]
    [InlineData("Wall", true)]
    [InlineData("wallPaper", true)]
    [InlineData("WALL", true)]
    [InlineData("123", false)]
    public void CaseSensitive_FollowsSmartCase(string pattern, bool expected)
    {
        Assert.Equal(expected, TestOptions.Create(pattern).CaseSensitive);
    }

    [Fact]
    public void CaseSensitive_ForcedFlagOverridesSmartCase()
    {
        Assert.True(TestOptions.Create("wall", forceCaseSensitive: true).CaseSensitive);
    }

    [Fact]
    public void SizeLimitDisabled_OnlyWhenLimitIsMaximumValue()
    {
        Assert.True(TestOptions.Create("wall", maxFileSize: long.MaxValue).SizeLimitDisabled);
        Assert.False(TestOptions.Create("wall", maxFileSize: long.MaxValue - 1).SizeLimitDisabled);
    }
}
