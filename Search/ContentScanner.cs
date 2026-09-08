using System.Text;
using DLSearch.Cli;

namespace DLSearch.Search;

public sealed class ContentScanner
{
    private const int MinimumBufferSize = 256 * 1024;
    private const int MaximumLineBytes = 4096;
    private const byte NewLine = 10;
    private const byte CarriageReturn = 13;

    private readonly PatternSet _patterns;
    private readonly SearchOptions _options;
    private readonly SearchStatistics _statistics;
    private readonly byte[] _buffer;
    private readonly List<Hit> _hits = [];

    public ContentScanner(PatternSet patterns, SearchOptions options, SearchStatistics statistics)
    {
        _patterns = patterns;
        _options = options;
        _statistics = statistics;
        _buffer = new byte[Math.Max(MinimumBufferSize, patterns.MaxPatternLength * 4)];
    }

    public ContentMatch? Scan(FileCandidate candidate, CancellationToken token)
    {
        if (!_options.SizeLimitDisabled && candidate.Length > _options.MaxFileSize)
        {
            _statistics.CountSkipped(SkipReason.Size);
            return null;
        }

        try
        {
            using var stream = new FileStream(
                candidate.FullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                0,
                FileOptions.SequentialScan);

            int total = ReadFull(stream, 0, _buffer.Length);
            if (total == 0)
            {
                _statistics.CountRead(0);
                return null;
            }

            TextKind kind = TextDetection.Detect(_buffer.AsSpan(0, total));

            if (kind == TextKind.Binary)
            {
                _statistics.CountSkipped(SkipReason.Binary);
                return null;
            }

            List<LineMatch> lines = kind == TextKind.Text
                ? ScanBytes(stream, total, token)
                : ScanUtf16(stream, TextDetection.EncodingFor(kind), token);

            if (lines.Count == 0)
            {
                return null;
            }

            _statistics.CountContentFile(lines.Count);
            return new ContentMatch(Path.Combine(candidate.RelativeDirectory, candidate.Name), lines);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _statistics.CountSkipped(SkipReason.Denied);
            return null;
        }
    }

    private List<LineMatch> ScanBytes(FileStream stream, int initialTotal, CancellationToken token)
    {
        var results = new List<LineMatch>();
        int carry = Math.Max(0, _patterns.MaxPatternLength - 1);
        int total = initialTotal;
        int keep = 0;
        long lineBase = 0;
        long bytesRead = initialTotal;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            CollectMatches(_buffer.AsSpan(0, total), keep, lineBase, results);

            if (total < _buffer.Length)
            {
                break;
            }

            int keepNext = Math.Min(carry, total);
            lineBase += CountNewlines(_buffer.AsSpan(0, total - keepNext));
            Buffer.BlockCopy(_buffer, total - keepNext, _buffer, 0, keepNext);
            keep = keepNext;

            int read = ReadFull(stream, keep, _buffer.Length - keep);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
            total = keep + read;
        }

        _statistics.CountRead(bytesRead);
        return results;
    }

    private List<LineMatch> ScanUtf16(FileStream stream, Encoding encoding, CancellationToken token)
    {
        var results = new List<LineMatch>();
        stream.Position = 0;

        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 16384, leaveOpen: true);

        long lineNumber = 0;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            token.ThrowIfCancellationRequested();
            lineNumber++;

            List<CharRange>? highlights = FindAll(line);
            if (highlights is not null)
            {
                results.Add(new LineMatch(lineNumber, line, highlights));
            }
        }

        _statistics.CountRead(stream.Length);
        return results;
    }

    private List<CharRange>? FindAll(string line)
    {
        List<CharRange>? highlights = null;
        int position = 0;

        while (position <= line.Length - _patterns.Text.Length)
        {
            int index = line.IndexOf(_patterns.Text, position, _patterns.Comparison);
            if (index < 0)
            {
                break;
            }

            highlights ??= [];
            highlights.Add(new CharRange(index, _patterns.Text.Length));
            position = index + 1;
        }

        return highlights;
    }

    private void CollectMatches(ReadOnlySpan<byte> span, int keep, long lineBase, List<LineMatch> results)
    {
        _hits.Clear();

        foreach (BytePattern pattern in _patterns.Patterns)
        {
            int position = 0;

            while (true)
            {
                int index = pattern.IndexOf(span, position);
                if (index < 0)
                {
                    break;
                }

                if (index + pattern.Length > keep)
                {
                    _hits.Add(new Hit(index, pattern));
                }

                position = index + 1;
            }
        }

        if (_hits.Count == 0)
        {
            return;
        }

        _hits.Sort(static (left, right) => left.Position.CompareTo(right.Position));

        long newlines = lineBase;
        int counted = 0;
        int current = 0;

        while (current < _hits.Count)
        {
            Hit hit = _hits[current];
            newlines += CountNewlines(span[counted..hit.Position]);
            counted = hit.Position;

            int lineStart = LineStart(span, hit.Position);
            int lineEnd = LineEnd(span, hit.Position);
            PatternEncoding encoding = hit.Pattern.Encoding;

            var highlights = new List<CharRange>();
            int next = current;

            while (next < _hits.Count && _hits[next].Position < lineEnd)
            {
                Hit grouped = _hits[next];
                int visibleLength = Math.Min(grouped.Pattern.Length, lineEnd - grouped.Position);
                int highlightStart = encoding.CountChars(span[lineStart..grouped.Position]);
                int highlightLength = encoding.CountChars(span.Slice(grouped.Position, visibleLength));
                highlights.Add(new CharRange(highlightStart, highlightLength));
                next++;
            }

            results.Add(new LineMatch(newlines + 1, Decode(encoding, span[lineStart..lineEnd]), highlights));
            current = next;
        }
    }

    private static string Decode(PatternEncoding encoding, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 0 && bytes[^1] == CarriageReturn)
        {
            bytes = bytes[..^1];
        }

        return encoding.Decode(bytes);
    }

    private static int LineStart(ReadOnlySpan<byte> span, int position)
    {
        int start = span[..position].LastIndexOf(NewLine) + 1;
        return Math.Max(start, position - MaximumLineBytes);
    }

    private static int LineEnd(ReadOnlySpan<byte> span, int position)
    {
        int relative = span[position..].IndexOf(NewLine);
        int end = relative < 0 ? span.Length : position + relative;
        return Math.Min(end, position + MaximumLineBytes);
    }

    private static long CountNewlines(ReadOnlySpan<byte> span)
    {
        return span.Count(NewLine);
    }

    private int ReadFull(FileStream stream, int offset, int count)
    {
        int filled = 0;

        while (filled < count)
        {
            int read = stream.Read(_buffer, offset + filled, count - filled);
            if (read == 0)
            {
                break;
            }

            filled += read;
        }

        return filled;
    }

    private readonly record struct Hit(int Position, BytePattern Pattern);
}
