using System.Text.RegularExpressions;

namespace DLSearch.Tests.Infrastructure;

public static partial class AnsiText
{
    [GeneratedRegex(@"\x1B\[[0-9;]*[a-zA-Z]")]
    private static partial Regex ControlSequence();

    public static string Strip(string text) => ControlSequence().Replace(text, string.Empty).Replace("\r", string.Empty);
}
