using DLSearch.Cli;

namespace DLSearch.Tests.Cli;

public class ArgumentParserTests
{
    private const long Megabyte = 1024L * 1024L;

    [Fact]
    public void Parse_WithoutArguments_ReturnsUsage()
    {
        ParseResult result = ArgumentParser.Parse([]);

        Assert.Equal(ParseStatus.Usage, result.Status);
        Assert.Null(result.Options);
        Assert.Equal(UsageText.Line, result.Message!);
    }

    [Fact]
    public void Parse_FlagsWithoutPattern_ReturnsUsage()
    {
        ParseResult result = ArgumentParser.Parse(["-a", "-h", "-s"]);

        Assert.Equal(ParseStatus.Usage, result.Status);
    }

    [Fact]
    public void Parse_EmptyPattern_ReturnsUsage()
    {
        ParseResult result = ArgumentParser.Parse([""]);

        Assert.Equal(ParseStatus.Usage, result.Status);
    }

    [Fact]
    public void Parse_SinglePattern_ReturnsOptions()
    {
        SearchOptions options = Success(["wall"]);

        Assert.Equal("wall", options.Pattern);
        Assert.Null(options.Extensions);
        Assert.Equal(32 * Megabyte, options.MaxFileSize);
        Assert.False(options.IncludeJunkDirectories);
        Assert.False(options.IncludeHidden);
        Assert.False(options.ForceCaseSensitive);
    }

    [Fact]
    public void Parse_MultiplePatternWords_JoinsWithSingleSpace()
    {
        SearchOptions options = Success(["wall", "paper", "roll"]);

        Assert.Equal("wall paper roll", options.Pattern);
    }

    [Fact]
    public void Parse_FlagAfterPatternStart_BelongsToPattern()
    {
        SearchOptions options = Success(["wall", "-e", "cs"]);

        Assert.Equal("wall -e cs", options.Pattern);
        Assert.Null(options.Extensions);
    }

    [Fact]
    public void Parse_DoubleDash_EndsFlagParsing()
    {
        SearchOptions options = Success(["--", "-e"]);

        Assert.Equal("-e", options.Pattern);
        Assert.Null(options.Extensions);
    }

    [Fact]
    public void Parse_DoubleDashAfterFlags_KeepsEarlierFlags()
    {
        SearchOptions options = Success(["-a", "--", "-M"]);

        Assert.Equal("-M", options.Pattern);
        Assert.True(options.IncludeJunkDirectories);
        Assert.False(options.SizeLimitDisabled);
    }

    [Fact]
    public void Parse_SingleDash_IsPartOfPattern()
    {
        SearchOptions options = Success(["-"]);

        Assert.Equal("-", options.Pattern);
    }

    [Fact]
    public void Parse_Extensions_AddsLeadingDot()
    {
        SearchOptions options = Success(["-e", "cs,md", "wall"]);

        Assert.NotNull(options.Extensions);
        Assert.Equal(2, options.Extensions!.Count);
        Assert.Contains(".cs", options.Extensions);
        Assert.Contains(".md", options.Extensions);
    }

    [Fact]
    public void Parse_Extensions_IgnoresCaseAndSurroundingSpaces()
    {
        SearchOptions options = Success(["-e", ".CS, .Md", "wall"]);

        Assert.Contains(".cs", options.Extensions!, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(".MD", options.Extensions!, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DuplicateExtensions_AreCollapsed()
    {
        SearchOptions options = Success(["-e", "cs,.cs,CS", "wall"]);

        Assert.Single(options.Extensions!);
    }

    [Fact]
    public void Parse_ExtensionsWithoutValue_ReturnsError()
    {
        ParseResult result = ArgumentParser.Parse(["-e"]);

        Assert.Equal(ParseStatus.Error, result.Status);
        Assert.Contains("-e", result.Message!);
    }

    [Theory]
    [InlineData(",,,")]
    [InlineData(".")]
    public void Parse_ExtensionsWithoutUsableValues_ReturnsError(string value)
    {
        ParseResult result = ArgumentParser.Parse(["-e", value, "wall"]);

        Assert.Equal(ParseStatus.Error, result.Status);
    }

    [Fact]
    public void Parse_MaxSize_ConvertsMegabytesToBytes()
    {
        SearchOptions options = Success(["-m", "8", "wall"]);

        Assert.Equal(8 * Megabyte, options.MaxFileSize);
        Assert.False(options.SizeLimitDisabled);
    }

    [Fact]
    public void Parse_MaxSizeWithoutValue_ReturnsError()
    {
        ParseResult result = ArgumentParser.Parse(["-m"]);

        Assert.Equal(ParseStatus.Error, result.Status);
        Assert.Contains("-m", result.Message!);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("+8")]
    public void Parse_InvalidMaxSize_ReturnsError(string value)
    {
        ParseResult result = ArgumentParser.Parse(["-m", value, "wall"]);

        Assert.Equal(ParseStatus.Error, result.Status);
        Assert.Contains(value, result.Message!);
    }

    [Fact]
    public void Parse_DisabledSizeLimit_WinsOverExplicitMaxSize()
    {
        Assert.True(Success(["-m", "8", "-M", "wall"]).SizeLimitDisabled);
        Assert.True(Success(["-M", "-m", "8", "wall"]).SizeLimitDisabled);
    }

    [Fact]
    public void Parse_DisabledSizeLimit_UsesMaximumValue()
    {
        Assert.Equal(long.MaxValue, Success(["-M", "wall"]).MaxFileSize);
    }

    [Fact]
    public void Parse_BooleanFlags_AreApplied()
    {
        SearchOptions options = Success(["-a", "-h", "-s", "wall"]);

        Assert.True(options.IncludeJunkDirectories);
        Assert.True(options.IncludeHidden);
        Assert.True(options.ForceCaseSensitive);
    }

    [Fact]
    public void Parse_RepeatedFlag_IsAccepted()
    {
        SearchOptions options = Success(["-a", "-a", "wall"]);

        Assert.True(options.IncludeJunkDirectories);
    }

    [Fact]
    public void Parse_LastExtensionsFlag_Wins()
    {
        SearchOptions options = Success(["-e", "cs", "-e", "md", "wall"]);

        Assert.Single(options.Extensions!);
        Assert.Contains(".md", options.Extensions!);
    }

    [Fact]
    public void Parse_UnknownFlag_ReturnsErrorWithUsage()
    {
        ParseResult result = ArgumentParser.Parse(["-x", "wall"]);

        Assert.Equal(ParseStatus.Error, result.Status);
        Assert.Contains("-x", result.Message!);
        Assert.Contains(UsageText.Line, result.Message!);
    }

    private static SearchOptions Success(string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        Assert.Equal(ParseStatus.Success, result.Status);
        Assert.NotNull(result.Options);
        return result.Options!;
    }
}
