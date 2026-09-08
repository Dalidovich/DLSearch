using System.Globalization;

namespace DLSearch.Output;

public static class Formatting
{
    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];

    private static readonly NumberFormatInfo GroupedNumbers = CreateGroupedNumbers();

    public static string Count(long value)
    {
        return value.ToString("#,0", GroupedNumbers);
    }

    public static string Size(long bytes)
    {
        double value = bytes;
        int unit = 0;

        while (value >= 1024 && unit < SizeUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        string number = unit == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);

        return number + " " + SizeUnits[unit];
    }

    public static string Duration(TimeSpan elapsed)
    {
        return elapsed.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds)
            : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", elapsed.Minutes, elapsed.Seconds);
    }

    public static string Date(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return "-";
        }

        DateTime today = DateTime.Today;

        if (value.Date == today)
        {
            return value.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (value.Date == today.AddDays(-1))
        {
            return "yesterday";
        }

        return value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    private static NumberFormatInfo CreateGroupedNumbers()
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NumberGroupSeparator = " ";
        return format;
    }
}
