using System.Globalization;

namespace DLSearch.Cli;

public static class ArgumentParser
{
    private const long DefaultMaxMegabytes = 32;

    public static ParseResult Parse(string[] args)
    {
        HashSet<string>? extensions = null;
        long maxMegabytes = DefaultMaxMegabytes;
        bool sizeLimitDisabled = false;
        bool includeJunk = false;
        bool includeHidden = false;
        bool caseSensitive = false;
        bool flagsEnded = false;

        var queryParts = new List<string>();
        int index = 0;

        while (index < args.Length)
        {
            string argument = args[index];

            if (!flagsEnded && argument == "--")
            {
                flagsEnded = true;
                index++;
                continue;
            }

            if (!flagsEnded && argument.Length > 1 && argument[0] == '-')
            {
                switch (argument)
                {
                    case "-e":
                        if (index + 1 >= args.Length)
                        {
                            return ParseResult.Error("Flag -e requires a comma separated list of extensions.");
                        }

                        extensions = ParseExtensions(args[index + 1]);
                        if (extensions.Count == 0)
                        {
                            return ParseResult.Error("Flag -e requires at least one extension.");
                        }

                        index += 2;
                        continue;

                    case "-m":
                        if (index + 1 >= args.Length)
                        {
                            return ParseResult.Error("Flag -m requires a size in megabytes.");
                        }

                        if (!long.TryParse(args[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out maxMegabytes) || maxMegabytes <= 0)
                        {
                            return ParseResult.Error($"Flag -m requires a positive number of megabytes, got '{args[index + 1]}'.");
                        }

                        index += 2;
                        continue;

                    case "-M":
                        sizeLimitDisabled = true;
                        index++;
                        continue;

                    case "-a":
                        includeJunk = true;
                        index++;
                        continue;

                    case "-h":
                        includeHidden = true;
                        index++;
                        continue;

                    case "-s":
                        caseSensitive = true;
                        index++;
                        continue;

                    default:
                        return ParseResult.Error($"Unknown flag '{argument}'. {UsageText.Line}");
                }
            }

            for (int rest = index; rest < args.Length; rest++)
            {
                queryParts.Add(args[rest]);
            }

            break;
        }

        string pattern = string.Join(' ', queryParts);
        if (pattern.Length == 0)
        {
            return ParseResult.Usage();
        }

        return ParseResult.Ok(new SearchOptions
        {
            Pattern = pattern,
            Extensions = extensions,
            MaxFileSize = sizeLimitDisabled ? long.MaxValue : maxMegabytes * 1024L * 1024L,
            IncludeJunkDirectories = includeJunk,
            IncludeHidden = includeHidden,
            ForceCaseSensitive = caseSensitive
        });
    }

    private static HashSet<string> ParseExtensions(string value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string normalized = part.StartsWith('.') ? part : '.' + part;
            if (normalized.Length > 1)
            {
                result.Add(normalized);
            }
        }

        return result;
    }
}
