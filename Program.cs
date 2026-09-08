using DLSearch.Cli;
using DLSearch.Output;
using DLSearch.Platform;
using DLSearch.Search;

namespace DLSearch;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleMode.Prepare();

        var surface = new ConsoleSurface();
        var printer = new ResultPrinter(surface);

        try
        {
            ParseResult parsed = ArgumentParser.Parse(args);

            switch (parsed.Status)
            {
                case ParseStatus.Usage:
                    printer.PrintUsage(parsed.Message!);
                    break;

                case ParseStatus.Error:
                    printer.PrintError(parsed.Message!);
                    break;

                default:
                    await SearchAsync(parsed.Options!, surface, printer).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception exception)
        {
            surface.ClearProgress();
            printer.PrintError(exception.Message);
        }

        WaitForKey();
    }

    private static async Task SearchAsync(SearchOptions options, ConsoleSurface surface, ResultPrinter printer)
    {
        string root;

        try
        {
            root = Directory.GetCurrentDirectory();
        }
        catch (Exception exception)
        {
            printer.PrintError("Cannot resolve the current directory: " + exception.Message);
            return;
        }

        if (!Directory.Exists(root))
        {
            printer.PrintError("Search root is not available: " + root);
            return;
        }

        var statistics = new SearchStatistics();
        var engine = new SearchEngine(options, PatternSet.Create(options.Pattern, options.CaseSensitive), statistics, surface, printer);

        using var cancellation = new CancellationTokenSource();

        void OnCancel(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            statistics.Interrupted = true;
            cancellation.Cancel();
        }

        Console.CancelKeyPress += OnCancel;

        try
        {
            await engine.RunAsync(root, cancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= OnCancel;
        }

        if (statistics.NameMatches == 0 && statistics.ContentMatches == 0)
        {
            printer.PrintNoResults();
        }

        printer.PrintStatistics(statistics, engine.Elapsed);
    }

    private static void WaitForKey()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return;
        }

        Console.ReadKey(true);
    }
}
