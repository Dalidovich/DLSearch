using System.Globalization;
using System.Text;
using DLSearch.Search;

namespace DLSearch.Output;

public sealed class ProgressIndicator
{
    private const char Separator = (char)0xB7;
    private const char FilledCell = (char)0x2588;
    private const char EmptyCell = (char)0x2591;
    private const int BarCells = 20;

    private static readonly char[] SpinnerFrames = BuildSpinnerFrames();

    private readonly SearchStatistics _statistics;
    private int _frame;

    public ProgressIndicator(SearchStatistics statistics)
    {
        _statistics = statistics;
    }

    public string Render(TimeSpan elapsed)
    {
        var builder = new StringBuilder(96);
        builder.Append(Ansi.Muted);

        long total = _statistics.TotalFiles;

        if (total < 0)
        {
            builder.Append(SpinnerFrames[_frame++ % SpinnerFrames.Length]);
            builder.Append(' ');
            Append(builder, Formatting.Count(_statistics.Directories), "dirs");
            builder.Append(' ').Append(Separator).Append(' ');
            Append(builder, Formatting.Count(_statistics.FilesSeen), "files");
        }
        else
        {
            int percent = total == 0 ? 100 : (int)Math.Min(100, _statistics.FilesProcessed * 100 / total);
            int filled = percent * BarCells / 100;

            builder.Append('[');
            builder.Append(FilledCell, filled);
            builder.Append(EmptyCell, BarCells - filled);
            builder.Append("] ");
            builder.Append(percent.ToString(CultureInfo.InvariantCulture)).Append("% ");
            builder.Append(Formatting.Count(_statistics.FilesProcessed)).Append('/').Append(Formatting.Count(total));
        }

        builder.Append(' ').Append(Separator).Append(' ');
        builder.Append(Formatting.Size(_statistics.BytesRead));
        builder.Append(' ').Append(Separator).Append(' ');
        Append(builder, Formatting.Count(_statistics.NameMatches + _statistics.ContentMatches), "matches");
        builder.Append(' ').Append(Separator).Append(' ');
        builder.Append(Formatting.Duration(elapsed));
        builder.Append(Ansi.Reset);

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string value, string label)
    {
        builder.Append(value).Append(' ').Append(label);
    }

    private static char[] BuildSpinnerFrames()
    {
        int[] codes = [0x280B, 0x2819, 0x2839, 0x2838, 0x283C, 0x2834, 0x2826, 0x2827, 0x2807, 0x280F];
        return [.. codes.Select(code => (char)code)];
    }
}
