using System.Threading.Channels;
using DLSearch.Cli;

namespace DLSearch.Search;

public sealed class DirectoryWalker
{
    private static readonly HashSet<string> JunkDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        ".vs",
        "packages"
    };

    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
        AttributesToSkip = 0,
        MatchType = MatchType.Simple
    };

    private readonly SearchOptions _options;
    private readonly PatternSet _patterns;
    private readonly SearchStatistics _statistics;

    public DirectoryWalker(SearchOptions options, PatternSet patterns, SearchStatistics statistics)
    {
        _options = options;
        _patterns = patterns;
        _statistics = statistics;
    }

    public async Task<List<NameMatch>> WalkAsync(string root, ChannelWriter<FileCandidate> writer, CancellationToken token)
    {
        var names = new List<NameMatch>();
        var pending = new Stack<string>();
        pending.Push(root);

        try
        {
            await EnumerateAsync(root, pending, names, writer, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        return names;
    }

    private async Task EnumerateAsync(string root, Stack<string> pending, List<NameMatch> names, ChannelWriter<FileCandidate> writer, CancellationToken token)
    {
        while (pending.Count > 0 && !token.IsCancellationRequested)
        {
            string directory = pending.Pop();
            _statistics.CountDirectory();

            string relativeDirectory = RelativeDirectory(root, directory);

            foreach (FileSystemInfo entry in Enumerate(directory))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                FileAttributes attributes = SafeAttributes(entry);

                if (!_options.IncludeHidden && (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if (!_options.IncludeJunkDirectories && JunkDirectories.Contains(entry.Name))
                    {
                        continue;
                    }

                    pending.Push(entry.FullName);
                    continue;
                }

                if (entry is not FileInfo file || !MatchesExtensionFilter(file.Name))
                {
                    continue;
                }

                _statistics.CountFile();

                long length = SafeLength(file);
                AddNameMatch(names, relativeDirectory, file, length);

                await writer.WriteAsync(new FileCandidate(file.FullName, relativeDirectory, file.Name, length), token).ConfigureAwait(false);
            }
        }
    }

    private void AddNameMatch(List<NameMatch> names, string relativeDirectory, FileInfo file, long length)
    {
        int index = file.Name.IndexOf(_patterns.Text, _patterns.Comparison);
        if (index < 0)
        {
            return;
        }

        _statistics.CountNameMatch();
        names.Add(new NameMatch(relativeDirectory, file.Name, length, SafeModified(file), new CharRange(index, _patterns.Text.Length)));
    }

    private bool MatchesExtensionFilter(string name)
    {
        if (_options.Extensions is null)
        {
            return true;
        }

        string extension = Path.GetExtension(name);
        return extension.Length > 0 && _options.Extensions.Contains(extension);
    }

    private IEnumerable<FileSystemInfo> Enumerate(string directory)
    {
        IEnumerator<FileSystemInfo> enumerator;

        try
        {
            enumerator = new DirectoryInfo(directory).EnumerateFileSystemInfos("*", Options).GetEnumerator();
        }
        catch (Exception exception) when (IsAccessFailure(exception))
        {
            _statistics.CountDenied();
            yield break;
        }

        using (enumerator)
        {
            while (true)
            {
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        yield break;
                    }
                }
                catch (Exception exception) when (IsAccessFailure(exception))
                {
                    _statistics.CountDenied();
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
    }

    private static bool IsAccessFailure(Exception exception)
    {
        return exception is UnauthorizedAccessException or IOException or System.Security.SecurityException;
    }

    private static FileAttributes SafeAttributes(FileSystemInfo entry)
    {
        try
        {
            return entry.Attributes;
        }
        catch (Exception exception) when (IsAccessFailure(exception))
        {
            return entry is DirectoryInfo ? FileAttributes.Directory : FileAttributes.Normal;
        }
    }

    private static long SafeLength(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch (Exception exception) when (IsAccessFailure(exception))
        {
            return 0;
        }
    }

    private static DateTime SafeModified(FileInfo file)
    {
        try
        {
            return file.LastWriteTime;
        }
        catch (Exception exception) when (IsAccessFailure(exception))
        {
            return DateTime.MinValue;
        }
    }

    private static string RelativeDirectory(string root, string directory)
    {
        string relative = Path.GetRelativePath(root, directory);
        return relative == "." ? string.Empty : relative;
    }
}
