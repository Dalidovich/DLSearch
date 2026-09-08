using System.Diagnostics;
using System.Threading.Channels;
using DLSearch.Cli;
using DLSearch.Output;

namespace DLSearch.Search;

public sealed class SearchEngine
{
    private const int FileQueueCapacity = 8192;
    private const int EventQueueCapacity = 256;
    private const int ProgressIntervalMilliseconds = 50;

    private readonly SearchOptions _options;
    private readonly PatternSet _patterns;
    private readonly SearchStatistics _statistics;
    private readonly ConsoleSurface _surface;
    private readonly ResultPrinter _printer;
    private readonly Stopwatch _clock = new();

    public SearchEngine(SearchOptions options, PatternSet patterns, SearchStatistics statistics, ConsoleSurface surface, ResultPrinter printer)
    {
        _options = options;
        _patterns = patterns;
        _statistics = statistics;
        _surface = surface;
        _printer = printer;
    }

    public TimeSpan Elapsed => _clock.Elapsed;

    public async Task RunAsync(string root, CancellationToken token)
    {
        _clock.Start();

        var files = Channel.CreateBounded<FileCandidate>(new BoundedChannelOptions(FileQueueCapacity)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var events = Channel.CreateBounded<OutputEvent>(new BoundedChannelOptions(EventQueueCapacity)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        using var progressStop = new CancellationTokenSource();

        Task printing = Task.Run(() => PrintLoopAsync(events.Reader), CancellationToken.None);
        Task progress = Task.Run(() => ProgressLoopAsync(progressStop.Token), CancellationToken.None);

        var workers = new Task[Environment.ProcessorCount];
        for (int i = 0; i < workers.Length; i++)
        {
            workers[i] = Task.Run(() => ScanLoopAsync(files.Reader, events.Writer, token), CancellationToken.None);
        }

        var walker = new DirectoryWalker(_options, _patterns, _statistics);
        IReadOnlyList<NameMatch> names;

        try
        {
            names = await walker.WalkAsync(root, files.Writer, token).ConfigureAwait(false);
        }
        finally
        {
            files.Writer.Complete();
        }

        _statistics.FreezeTotalFiles();
        await events.Writer.WriteAsync(new WalkFinished(names), CancellationToken.None).ConfigureAwait(false);

        await Task.WhenAll(workers).ConfigureAwait(false);
        events.Writer.Complete();
        await printing.ConfigureAwait(false);

        await progressStop.CancelAsync().ConfigureAwait(false);
        await progress.ConfigureAwait(false);

        _surface.ClearProgress();
        _clock.Stop();
    }

    private async Task ScanLoopAsync(ChannelReader<FileCandidate> reader, ChannelWriter<OutputEvent> writer, CancellationToken token)
    {
        var scanner = new ContentScanner(_patterns, _options, _statistics);

        try
        {
            await foreach (FileCandidate candidate in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                ContentMatch? match = scanner.Scan(candidate, token);
                _statistics.CountProcessed();

                if (match is not null)
                {
                    await writer.WriteAsync(new FileMatched(match), CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PrintLoopAsync(ChannelReader<OutputEvent> reader)
    {
        var pending = new List<ContentMatch>();
        bool namesPrinted = false;

        await foreach (OutputEvent item in reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
        {
            switch (item)
            {
                case WalkFinished walk:
                    _printer.PrintNames(walk.Names);
                    namesPrinted = true;

                    foreach (ContentMatch buffered in pending)
                    {
                        _printer.PrintContent(buffered);
                    }

                    pending.Clear();
                    break;

                case FileMatched matched when namesPrinted:
                    _printer.PrintContent(matched.Match);
                    break;

                case FileMatched matched:
                    pending.Add(matched.Match);
                    break;
            }
        }
    }

    private async Task ProgressLoopAsync(CancellationToken token)
    {
        if (!_surface.Decorated)
        {
            return;
        }

        var indicator = new ProgressIndicator(_statistics);

        try
        {
            while (!token.IsCancellationRequested)
            {
                _surface.SetProgress(indicator.Render(_clock.Elapsed));
                await Task.Delay(ProgressIntervalMilliseconds, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
